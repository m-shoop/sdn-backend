using SdnBackend.Dtos;
using SdnBackend.Models;
using SdnBackend.Repositories;
using Npgsql;

namespace SdnBackend.Services;

public class AvailabilityService(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    /// <summary>
    /// Fetches available time slots for a given service at a salon within a date range.
    /// Returns a tuple of (slots, errorMessage). If errorMessage is non-null, the request failed.
    /// </summary>
    public async Task<(List<TechAvailableSlotsOnDateDto>? Slots, string? ErrorMessage)> GetAvailableSlots(
        AvailabilityRequest request)
    {
        // Validate salon and schedules
        var salonData = await GetSalonAndSchedules(request.Salon);
        if (salonData is null)
            return (null, "Salon or schedules not found");

        var listSchedules = salonData.Schedules;

        // Validate and fetch service
        var service = await GetService(request.Service);
        if (service is null)
            return (null, "Service not found");

        // Validate dates
        DateOnly startDate = DateOnly.ParseExact(request.DateBegin, "yyyy-MM-dd");
        DateOnly endDate = DateOnly.ParseExact(request.DateEnd, "yyyy-MM-dd");

        if (endDate < startDate)
            return (null, "End date must be after start date");

        var serviceDto = new ServiceDto(service.Id, service.Name, service.Duration);
        var agreeRepo = new AgreementRepository(_dataSource);
        List<TechAvailableSlotsOnDateDto> techAvailableSlotsList = new();

        for (DateOnly currDate = startDate; currDate.DayNumber <= endDate.DayNumber; currDate = currDate.AddDays(1))
        {
            foreach (Schedule schedule in listSchedules)
            {
                // Retrieve the tech's list of agreements on this date
                List<Agreement> techAgreementsOnDate =
                    await agreeRepo.GetActiveAgreementsForTechOnDate(currDate, schedule.TechAccount);

                // Pass in the tech's agreements on that date
                List<TimeOnly> availableTimes =
                    schedule.GetAvailableStartTimesOnDate(currDate, service.Duration, techAgreementsOnDate);

                if (availableTimes.Count > 0)
                {
                    techAvailableSlotsList.Add(new TechAvailableSlotsOnDateDto(
                        schedule.TechAccount.Id,
                        currDate,
                        serviceDto,
                        availableTimes));
                }
            }
        }

        techAvailableSlotsList = TechAvailableSlotsOnDateDto.Normalize(techAvailableSlotsList);
        return (techAvailableSlotsList, null);
    }

    private async Task<SalonWithSchedules?> GetSalonAndSchedules(string salonIdRaw)
    {
        if (!int.TryParse(salonIdRaw, out int salonId))
            return null;

        var salonRepo = new SalonRepository(_dataSource);
        var salon = await salonRepo.GetById(salonId);
        if (salon is null)
            return null;

        var accountRepo = new AccountRepository(_dataSource);
        var scheduleRepo = new ScheduleRepository(_dataSource, accountRepo);
        var schedules = await scheduleRepo.GetSchedulesBySalon(salon);

        if (!schedules.Any())
            return null;

        return new SalonWithSchedules(salon, schedules);
    }

    private async Task<Service?> GetService(string rawServiceId)
    {
        if (!int.TryParse(rawServiceId, out int serviceId))
            return null;

        var serviceRepo = new ServiceRepository(_dataSource);
        return await serviceRepo.GetById(serviceId);
    }
}
