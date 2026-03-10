namespace SdnBackend.Dtos;

public record DayTimeRangeDto(string BeginTime, string EndTime);

public record ScheduleSummaryDto(
    int Id,
    string EffStartDate,
    string? EffEndDate,
    bool Outage,
    List<DayTimeRangeDto> TimeRangesForDay
);

public record AppointmentSummaryDto(
    int Id,
    string Date,
    string Time,
    string ServiceName,
    int ServiceDuration,
    string ClientName,
    string Status
);

public record CalendarDayResponseDto(
    List<ScheduleSummaryDto> Schedules,
    List<AppointmentSummaryDto> Appointments
);
