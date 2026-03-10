namespace SdnBackend.Models;

public class Salon(int id)
{
    // pass in int id
    public int Id => id;
    // for now, default to Shooper Dooper Nails
    public string Name => "Shooper Dooper Nails";
    // which always is Amsterdam time :) 
    public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    // override ToString to give id, name and timezone
    public override string ToString() => $"{Id}: {Name} (Timezone: {TimeZone})";
}

// helper record for loading a salon with its schedules
public record SalonWithSchedules(
    Salon Salon,
    List<Schedule> Schedules
);