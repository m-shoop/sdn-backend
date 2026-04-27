namespace SdnBackend.Models;

// beginTime - time when range of availability (or outage) begins
// endTime - time when range of availability (or outage) ends
// dayOfWeek - DayOfWeek object from DateOnly object
public class DayTimeRange(int dayTimeRangeId, TimeOnly beginTime, TimeOnly endTime, DayOfWeek dayOfWeek)
{
    public int DayTimeRangeId { get; init; } = dayTimeRangeId;
    public TimeOnly BeginTime { get; init; } = beginTime;
    public TimeOnly EndTime { get; init; } = endTime; 
    public DayOfWeek Day { get; init; } = dayOfWeek;
}