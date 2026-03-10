using SdnBackend.Dtos;
using SdnBackend.Models;
using SdnBackend.Repositories;
using Npgsql;

namespace SdnBackend.Services;

public class SalonServicesService(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    /// <summary>
    /// Fetches all technicians and their services for a given salon.
    /// Returns null if the salon or its schedules are not found.
    /// </summary>
    public async Task<PullSalonTechnicianServicesResponseDto?> GetSalonTechnicianServices(string salonIdRaw)
    {
        var salonData = await GetSalonAndSchedules(salonIdRaw);

        if (salonData is null)
            return null;

        var salon = salonData.Salon;
        var listSchedules = salonData.Schedules;

        // Build tech -> services dictionary from schedules
        Dictionary<Technician, List<Service>> techServiceDictionary = new();

        foreach (Schedule schedule in listSchedules)
        {
            if (!techServiceDictionary.ContainsKey(schedule.TechAccount))
            {
                techServiceDictionary.Add(schedule.TechAccount, schedule.TechAccount.Services);
            }
        }

        // Convert to DTOs
        List<TechnicianServicesDto> listTechServicesDto = new();
        foreach (var techServices in techServiceDictionary)
        {
            List<ServiceDto> listServicesDto = new();
            foreach (Service service in techServices.Value)
            {
                listServicesDto.Add(new ServiceDto(service.Id, service.Name, service.Duration));
            }

            listTechServicesDto.Add(new TechnicianServicesDto(
                techServices.Key.Id,
                techServices.Key.Name,
                listServicesDto));
        }

        return new PullSalonTechnicianServicesResponseDto(salon.Id, listTechServicesDto);
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
}
