namespace SdnBackend.Dtos;

public class TechAvailableSlotsOnDateDto(int techId, DateOnly date, ServiceDto serviceDto, List<TimeOnly> availableStartTimes)
{
    public int TechId { get; set; } = techId;

    public DateOnly Date { get; set; } = date;

    public ServiceDto ServiceDto { get; set; } = serviceDto;

    public List<TimeOnly> AvailableStartTimes { get; set; } = availableStartTimes;

    /// <summary>
    /// Merges the available start times of a group of TechAvailableSlotsOnDateDto objects
    /// Taken from ChatGPT
    /// </summary>
    /// <param name="group"></param>
    /// <returns>List of the available start times, de-duplicated</returns>
    private static List<TimeOnly> MergeStartTimes(IEnumerable<TechAvailableSlotsOnDateDto> group)
    {
        return group
            .SelectMany(d => d.AvailableStartTimes)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    /// <summary>
    /// Normalizes a list of TechAvailableSlotsOnDateDto objects so that
    /// it is ordered by date, then by tech, and all available slots for that
    /// combination are de-duplicated.
    /// Taken from ChatGPT
    /// </summary>
    /// <param name="rawDtos"></param>
    /// <returns></returns>
    public static List<TechAvailableSlotsOnDateDto> Normalize(
        IEnumerable<TechAvailableSlotsOnDateDto> rawDtos)
    {
        return rawDtos.GroupBy(dto => new
        {
            dto.TechId,
            dto.Date,
            ServiceId = dto.ServiceDto.Id
        })
        .Select(group =>
        {
            var service = group
                .Select(d => d.ServiceDto)
                .First();
            
            return new TechAvailableSlotsOnDateDto(
                group.Key.TechId,
                group.Key.Date,
                service,
                MergeStartTimes(group)
            );
        })
        .OrderBy(dto => dto.Date)
        .ThenBy(dto => dto.TechId)
        .ToList();
    }
}