namespace SdnBackend.Dtos;

public class PullSalonTechnicianServicesResponseDto(int salonId, List<TechnicianServicesDto> techServDtoList)
{
    public int SalonId { get; set; } = salonId;
    public List<TechnicianServicesDto> TechServDtoList { get; set; } = techServDtoList;
}