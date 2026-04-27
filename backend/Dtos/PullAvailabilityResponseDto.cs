namespace SdnBackend.Dtos;

public class PullAvailabilityResponseDto(int salonId, int serviceId, List<TechAvailableSlotsOnDateDto> techAvailableSlotsOnDateList)
{
    public int SalonId { get; set; } = salonId;

    public int ServiceId { get; set; } = serviceId;

    public List<TechAvailableSlotsOnDateDto> TechAvailableSlotsOnDateList = techAvailableSlotsOnDateList;
}