namespace SdnBackend.Dtos;

public class TechnicianServicesDto(int techId, string techName, List<ServiceDto> servicesDtoList)
{
    public int TechId { get; set; } = techId;
    public string TechName { get; set; } = techName;
    public List<ServiceDto> ServicesDtoList { get; set; }= servicesDtoList;
}