namespace SdnBackend.Models;

public abstract record Account(int Id, string Name)
{
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public abstract string Role { get; }
}

public record Technician(int Id, string Name, string Email) : Account(Id, Name)
{
    public override string Role => "Technician";

    public List<Service> Services { get; set; } = new List<Service>();
}

public record Client(int Id, string Name, string Email) : Account(Id, Name)
{
    public override string Role => "Client";
}

public record Admin(int Id, string Name, string[] Permissions) : Account(Id, Name)
{
    public override string Role => "Administrator";
}
