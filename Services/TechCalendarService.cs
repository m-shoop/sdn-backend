using Npgsql;
using SdnBackend.Dtos;
using SdnBackend.Models;
using SdnBackend.Repositories;

namespace SdnBackend.Services;

public class TechCalendarService(NpgsqlDataSource dataSource)
{
    private readonly ScheduleRepository _scheduleRepo = new(dataSource, new AccountRepository(dataSource));
    private readonly AgreementRepository _agreementRepo = new(dataSource);
    private readonly AccountRepository _accountRepo = new(dataSource);

    public async Task<CalendarDayResponseDto?> GetCalendarDay(int techId, DateOnly date)
    {
        // Fetch the technician account
        var account = await _accountRepo.GetById(techId);
        if (account is not Technician tech)
            return null;

        // Fetch all schedules for this technician
        var allSchedules = await _scheduleRepo.GetByTechId(techId);

        // Filter to schedules active on the requested date
        var activeSchedules = allSchedules.Where(s =>
            s.EffStartDate <= date &&
            (s.EffEndDate == default(DateOnly) || s.EffEndDate >= date)
        ).ToList();

        // Map schedules to DTOs, filtering DayTimeRanges to the requested day
        var scheduleDtos = activeSchedules.Select(s =>
        {
            var rangesForDay = s.DayTimeRanges
                .Where(r => r.Day == date.DayOfWeek)
                .Select(r => new DayTimeRangeDto(
                    r.BeginTime.ToString("HH:mm"),
                    r.EndTime.ToString("HH:mm")
                ))
                .ToList();

            return new ScheduleSummaryDto(
                s.Id,
                s.EffStartDate.ToString("yyyy-MM-dd"),
                s.EffEndDate == default(DateOnly) ? null : s.EffEndDate.ToString("yyyy-MM-dd"),
                s.Outage,
                rangesForDay
            );
        }).ToList();

        // Fetch active appointments for the technician on this date
        var agreements = await _agreementRepo.GetActiveAgreementsForTechOnDate(date, tech);

        var appointmentDtos = agreements.OrderBy(a => a.Time).Select(a => new AppointmentSummaryDto(
            a.Id,
            a.Date.ToString("yyyy-MM-dd"),
            a.Time.ToString("HH:mm"),
            a.Service.Name,
            a.Service.Duration,
            a.Client.Name,
            a.ApptStatus.ToString()
        )).ToList();

        return new CalendarDayResponseDto(scheduleDtos, appointmentDtos);
    }
}
