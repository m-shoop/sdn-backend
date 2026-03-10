namespace SdnBackend.Models;

public record Service(int Id, string Name, int Duration)
{    
    public int MaxParticipants { get; set; } = 1;
}