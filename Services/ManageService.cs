using Npgsql;
using SdnBackend.Dtos;
using SdnBackend.Models;
using SdnBackend.Repositories;

namespace SdnBackend.Services;

public class ManageService(NpgsqlDataSource dataSource, IEmailService emailService, ILogger<ManageService> logger)
{
    private readonly ScheduleRepository _scheduleRepo = new(dataSource, new AccountRepository(dataSource));
    private readonly AgreementRepository _agreementRepo = new(dataSource);
    private readonly ServiceRepository _serviceRepo = new(dataSource);
    private readonly AccountRepository _accountRepo = new(dataSource);
    private readonly SalonRepository _salonRepo = new(dataSource);
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<ManageService> _logger = logger;

    // ── Schedules ────────────────────────────────────────────────────────────

    public async Task<ScheduleDetailDto?> GetScheduleDetail(int scheduleId, int techId)
    {
        var schedule = await _scheduleRepo.GetById(scheduleId);
        if (schedule is null || schedule.TechAccount.Id != techId)
            return null;

        var timeRanges = schedule.DayTimeRanges
            .Select(r => new DayTimeRangeEditDto(
                r.Day.ToString(),
                r.BeginTime.ToString("HH:mm"),
                r.EndTime.ToString("HH:mm")
            ))
            .ToList();

        return new ScheduleDetailDto(
            schedule.Id,
            schedule.EffStartDate.ToString("yyyy-MM-dd"),
            schedule.EffEndDate == default(DateOnly) ? null : schedule.EffEndDate.ToString("yyyy-MM-dd"),
            schedule.Outage,
            timeRanges
        );
    }

    public async Task<(bool Success, string? Error)> UpdateSchedule(int scheduleId, int techId, UpdateScheduleRequest request)
    {
        var schedule = await _scheduleRepo.GetById(scheduleId);
        if (schedule is null || schedule.TechAccount.Id != techId)
            return (false, "Schedule not found.");

        if (!DateOnly.TryParse(request.EffStartDate, out var effStartDate))
            return (false, "Invalid effective start date.");

        DateOnly? effEndDate = null;
        if (!string.IsNullOrWhiteSpace(request.EffEndDate))
        {
            if (!DateOnly.TryParse(request.EffEndDate, out var parsedEnd))
                return (false, "Invalid effective end date.");
            effEndDate = parsedEnd;
        }

        var parsedRanges = new List<(DayOfWeek day, TimeOnly begin, TimeOnly end)>();
        foreach (var r in request.TimeRanges)
        {
            if (!Enum.TryParse<DayOfWeek>(r.Day, out var day))
                return (false, $"Invalid day of week: {r.Day}");
            if (!TimeOnly.TryParse(r.BeginTime, out var begin) || !TimeOnly.TryParse(r.EndTime, out var end))
                return (false, $"Invalid time range for {r.Day}.");
            if (begin >= end)
                return (false, $"Begin time must be before end time for {r.Day}.");
            parsedRanges.Add((day, begin, end));
        }

        await _scheduleRepo.UpdateScheduleFields(scheduleId, effStartDate, effEndDate, request.Outage);
        await _scheduleRepo.ReplaceTimeRanges(scheduleId, parsedRanges);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateSchedule(int scheduleId, int techId)
    {
        var schedule = await _scheduleRepo.GetById(scheduleId);
        if (schedule is null || schedule.TechAccount.Id != techId)
            return (false, "Schedule not found.");

        await _scheduleRepo.DeactivateSchedule(scheduleId);
        return (true, null);
    }

    // ── Appointments ─────────────────────────────────────────────────────────

    public async Task<AppointmentDetailResponseDto?> GetAppointmentDetail(int apptId, int techId)
    {
        var agreement = await _agreementRepo.GetById(apptId);
        if (agreement is null || agreement.Tech.Id != techId)
            return null;

        var apptDto = new AppointmentDetailDto(
            agreement.Id,
            agreement.Date.ToString("yyyy-MM-dd"),
            agreement.Time.ToString("HH:mm"),
            agreement.Service.Id,
            agreement.Service.Name,
            agreement.Service.Duration,
            agreement.Client.Name,
            agreement.ApptStatus.ToString()
        );

        var allServices = await _serviceRepo.GetAll();
        var serviceOptions = allServices
            .Select(s => new ServiceOptionDto(s.Id, s.Name, s.Duration))
            .ToList();

        return new AppointmentDetailResponseDto(apptDto, serviceOptions);
    }

    public async Task<(bool Success, string? Error, List<OverlapInfoDto> Overlaps)> UpdateAppointment(
        int apptId, int techId, UpdateAppointmentRequest request)
    {
        var agreement = await _agreementRepo.GetById(apptId);
        if (agreement is null || agreement.Tech.Id != techId)
            return (false, "Appointment not found.", []);

        if (!DateOnly.TryParse(request.Date, out var newDate))
            return (false, "Invalid date.", []);

        if (!TimeOnly.TryParse(request.Time, out var newTime))
            return (false, "Invalid time.", []);

        var newService = await _serviceRepo.GetById(request.ServiceId);
        if (newService is null)
            return (false, "Service not found.", []);

        // Check for overlapping appointments (excluding this one)
        var others = await _agreementRepo.GetActiveAgreementsForTechOnDateExcluding(newDate, agreement.Tech, apptId);
        var overlaps = new List<OverlapInfoDto>();
        var newEnd = newTime.AddMinutes(newService.Duration);

        foreach (var other in others)
        {
            var otherEnd = other.Time.AddMinutes(other.Service.Duration);
            bool overlaps_ = newTime < otherEnd && newEnd > other.Time;
            if (overlaps_)
            {
                overlaps.Add(new OverlapInfoDto(
                    other.Id,
                    other.Time.ToString("HH:mm"),
                    other.Client.Name,
                    other.Service.Name
                ));
            }
        }

        if (!request.ForceUpdate && overlaps.Count > 0)
            return (false, "Overlapping appointments detected.", overlaps);

        // Apply the update — Date/Time/Service are init-only so construct a new Agreement
        var updated = new Agreement
        {
            Id = agreement.Id,
            Date = newDate,
            Time = newTime,
            Service = newService,
            Tech = agreement.Tech,
            Client = agreement.Client,
            Salon = agreement.Salon,
            ApptStatus = agreement.ApptStatus,
            ExpireTimestamp = agreement.ExpireTimestamp,
            ConfirmTimestamp = agreement.ConfirmTimestamp,
            ConfirmTokenHash = agreement.ConfirmTokenHash,
            CreateTimestamp = agreement.CreateTimestamp
        };
        await _agreementRepo.UpdateEntity(updated);

        try
        {
            bool notifyEnabled = await _accountRepo.GetEmailNotificationPreference(techId);
            if (notifyEnabled)
            {
                await _emailService.SendTechNotificationAsync(new TechNotificationEmail
                {
                    To = agreement.Tech.Email,
                    NotificationType = "modified",
                    Date = newDate,
                    Time = newTime,
                    ServiceName = newService.Name,
                    ServiceDuration = newService.Duration,
                    OldDate = agreement.Date,
                    OldTime = agreement.Time,
                    OldServiceName = agreement.Service.Name,
                    OldServiceDuration = agreement.Service.Duration
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send modification notification to tech {TechId}", techId);
        }

        try
        {
            await _emailService.SendClientNotificationAsync(new ClientNotificationEmail
            {
                To = agreement.Client.Email,
                ClientName = agreement.Client.Name,
                NotificationType = "modified",
                Date = newDate,
                Time = newTime,
                ServiceName = newService.Name,
                ServiceDuration = newService.Duration,
                OldDate = agreement.Date,
                OldTime = agreement.Time,
                OldServiceName = agreement.Service.Name,
                OldServiceDuration = agreement.Service.Duration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send modification notification to client for appointment {ApptId}", apptId);
        }

        return (true, null, []);
    }

    public async Task<(bool Success, string? Error)> CancelAppointment(int apptId, int techId)
    {
        var agreement = await _agreementRepo.GetById(apptId);
        if (agreement is null || agreement.Tech.Id != techId)
            return (false, "Appointment not found.");

        await _agreementRepo.CancelAgreement(apptId);

        try
        {
            bool notifyEnabled = await _accountRepo.GetEmailNotificationPreference(techId);
            if (notifyEnabled)
            {
                await _emailService.SendTechNotificationAsync(new TechNotificationEmail
                {
                    To = agreement.Tech.Email,
                    NotificationType = "cancelled",
                    Date = agreement.Date,
                    Time = agreement.Time,
                    ServiceName = agreement.Service.Name,
                    ServiceDuration = agreement.Service.Duration
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cancellation notification to tech {TechId}", techId);
        }

        try
        {
            await _emailService.SendClientNotificationAsync(new ClientNotificationEmail
            {
                To = agreement.Client.Email,
                ClientName = agreement.Client.Name,
                NotificationType = "cancelled",
                Date = agreement.Date,
                Time = agreement.Time,
                ServiceName = agreement.Service.Name,
                ServiceDuration = agreement.Service.Duration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cancellation notification to client for appointment {ApptId}", apptId);
        }

        return (true, null);
    }

    // ── Create ───────────────────────────────────────────────────────────────

    public async Task<List<ServiceOptionDto>> GetAvailableServices()
    {
        var all = await _serviceRepo.GetAll();
        return all.Select(s => new ServiceOptionDto(s.Id, s.Name, s.Duration)).ToList();
    }

    public async Task<(bool Success, string? Error, int? NewId)> CreateSchedule(int techId, CreateScheduleRequest request)
    {
        if (!DateOnly.TryParse(request.EffStartDate, out var effStartDate))
            return (false, "Invalid effective start date.", null);

        DateOnly? effEndDate = null;
        if (!string.IsNullOrWhiteSpace(request.EffEndDate))
        {
            if (!DateOnly.TryParse(request.EffEndDate, out var parsedEnd))
                return (false, "Invalid effective end date.", null);
            effEndDate = parsedEnd;
        }

        var parsedRanges = new List<(DayOfWeek day, TimeOnly begin, TimeOnly end)>();
        foreach (var r in request.TimeRanges)
        {
            if (!Enum.TryParse<DayOfWeek>(r.Day, out var day))
                return (false, $"Invalid day of week: {r.Day}", null);
            if (!TimeOnly.TryParse(r.BeginTime, out var begin) || !TimeOnly.TryParse(r.EndTime, out var end))
                return (false, $"Invalid time range for {r.Day}.", null);
            if (begin >= end)
                return (false, $"Begin time must be before end time for {r.Day}.", null);
            parsedRanges.Add((day, begin, end));
        }

        int newId = await _scheduleRepo.CreateSchedule(techId, 1, effStartDate, effEndDate, request.Outage, parsedRanges);
        return (true, null, newId);
    }

    public async Task<(bool Success, string? Error, int? NewId, List<OverlapInfoDto> Overlaps)> CreateAppointment(int techId, CreateAppointmentRequest request)
    {
        if (!DateOnly.TryParse(request.Date, out var date))
            return (false, "Invalid date.", null, []);

        if (!TimeOnly.TryParse(request.Time, out var time))
            return (false, "Invalid time.", null, []);

        var service = await _serviceRepo.GetById(request.ServiceId);
        if (service is null)
            return (false, "Service not found.", null, []);

        var techAccount = await _accountRepo.GetById(techId);
        if (techAccount is not Technician tech)
            return (false, "Technician not found.", null, []);

        // Check for overlapping appointments
        var others = await _agreementRepo.GetActiveAgreementsForTechOnDate(date, tech);
        var overlaps = new List<OverlapInfoDto>();
        var newEnd = time.AddMinutes(service.Duration);
        foreach (var other in others)
        {
            var otherEnd = other.Time.AddMinutes(other.Service.Duration);
            if (time < otherEnd && newEnd > other.Time)
                overlaps.Add(new OverlapInfoDto(other.Id, other.Time.ToString("HH:mm"), other.Client.Name, other.Service.Name));
        }
        if (!request.ForceCreate && overlaps.Count > 0)
            return (false, "Overlapping appointments detected.", null, overlaps);

        Account? clientAccount = await _accountRepo.GetByEmailAndRole(request.ClientEmail, "Client");
        Client client;
        if (clientAccount is Client existingClient)
        {
            client = existingClient;
        }
        else
        {
            var newClient = new Client(0, request.ClientName, request.ClientEmail);
            await _accountRepo.SaveEntity(newClient);
            clientAccount = await _accountRepo.GetByEmailAndRole(request.ClientEmail, "Client");
            if (clientAccount is not Client createdClient)
                return (false, "Failed to create client.", null, []);
            client = createdClient;
        }

        var salon = await _salonRepo.GetById(1);
        if (salon is null)
            return (false, "Salon not found.", null, []);

        var agreement = new Agreement
        {
            Date = date,
            Time = time,
            Service = service,
            Tech = tech,
            Client = client,
            Salon = salon,
            ApptStatus = AppointmentStatus.confirmed
        };

        int newId = await _agreementRepo.CreateConfirmedAgreement(agreement);

        try
        {
            await _emailService.SendClientNotificationAsync(new ClientNotificationEmail
            {
                To = client.Email,
                ClientName = client.Name,
                NotificationType = "booked",
                Date = date,
                Time = time,
                ServiceName = service.Name,
                ServiceDuration = service.Duration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking notification to client {ClientEmail}", client.Email);
        }

        return (true, null, newId, []);
    }
}
