namespace SdnBackend.Dtos;

public class ServiceDto(int id, string name, int duration)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = name;
    public int Duration { get; set; } = duration;
}