namespace SdnBackend.Dtos;

// ── GET response DTOs ────────────────────────────────────────────────────────

public record DayTimeRangeEditDto(string Day, string BeginTime, string EndTime);

public record ScheduleDetailDto(
    int Id,
    string EffStartDate,
    string? EffEndDate,
    bool Outage,
    List<DayTimeRangeEditDto> TimeRanges
);

public record AppointmentDetailDto(
    int Id,
    string Date,
    string Time,
    int ServiceId,
    string ServiceName,
    int ServiceDuration,
    string ClientName,
    string Status
);

public record ServiceOptionDto(int Id, string Name, int Duration);

public record AppointmentDetailResponseDto(
    AppointmentDetailDto Appointment,
    List<ServiceOptionDto> AvailableServices
);

// ── Overlap info ─────────────────────────────────────────────────────────────

public record OverlapInfoDto(int ApptId, string Time, string ClientName, string ServiceName);

// ── PUT request DTOs ─────────────────────────────────────────────────────────

public record UpdateScheduleRequest(
    string EffStartDate,
    string? EffEndDate,
    bool Outage,
    List<DayTimeRangeEditDto> TimeRanges
);

public record UpdateAppointmentRequest(
    string Date,
    string Time,
    int ServiceId,
    bool ForceUpdate
);

// ── POST request DTOs ─────────────────────────────────────────────────────────

public record CreateScheduleRequest(
    string EffStartDate,
    string? EffEndDate,
    bool Outage,
    List<DayTimeRangeEditDto> TimeRanges
);

public record CreateAppointmentRequest(
    string Date,
    string Time,
    int ServiceId,
    string ClientName,
    string ClientEmail,
    bool ForceCreate
);
