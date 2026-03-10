using SdnBackend.Models;

namespace SdnBackend.Tests;

public class ScheduleTests
{
    // Wednesday 10:00–14:00, Friday 12:00–20:00
    // Date range: 2025-02-01 to 2026-10-31
    private static Schedule BuildSchedule()
    {
        var ranges = new List<DayTimeRange>
        {
            new DayTimeRange(1, new TimeOnly(10, 0), new TimeOnly(14, 0), DayOfWeek.Wednesday),
            new DayTimeRange(2, new TimeOnly(12, 0), new TimeOnly(20, 0), DayOfWeek.Friday)
        };

        return new Schedule(
            techAccount: new Technician(1, "Alice", "alice@example.com"),
            salon: new Salon(1),
            numDaysPriorReleased: 60,
            effStartDate: new DateOnly(2025, 2, 1),
            dayTimeRanges: ranges,
            frequency: 1
        )
        {
            Id = 1,
            EffEndDate = new DateOnly(2026, 10, 31),
            Outage = false
        };
    }

    private static Agreement BuildAgreement(DateOnly date, TimeOnly time, int serviceDuration) =>
        new()
        {
            Date = date,
            Time = time,
            Service = new Service(1, "Manicure", serviceDuration),
            Tech = new Technician(1, "Alice", "alice@example.com"),
            Client = new Client(2, "Bob", "bob@example.com"),
            Salon = new Salon(1)
        };

    // ── Date range boundary tests ─────────────────────────────────────────────

    [Fact]
    public void GetAvailableStartTimesOnDate_ReturnsEmpty_WhenDateBeforeEffStartDate()
    {
        var schedule = BuildSchedule();
        var date = new DateOnly(2024, 6, 1); // before 2025-02-01

        var result = schedule.GetAvailableStartTimesOnDate(date, 30, []);

        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableStartTimesOnDate_ReturnsEmpty_WhenDateAfterEffEndDate()
    {
        var schedule = BuildSchedule();
        var date = new DateOnly(2027, 1, 1); // after 2026-10-31

        var result = schedule.GetAvailableStartTimesOnDate(date, 30, []);

        Assert.Empty(result);
    }

    // ── Day-of-week tests ─────────────────────────────────────────────────────

    [Fact]
    public void GetAvailableStartTimesOnDate_ReturnsEmpty_WhenDayHasNoRange()
    {
        var schedule = BuildSchedule();
        var monday = new DateOnly(2026, 10, 12); // confirmed Monday, no range defined
        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);

        var result = schedule.GetAvailableStartTimesOnDate(monday, 30, []);

        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableStartTimesOnDate_ReturnsSlots_OnlyForMatchingDayOfWeek()
    {
        var schedule = BuildSchedule();
        var wednesday = new DateOnly(2026, 10, 7); // confirmed Wednesday
        var friday = new DateOnly(2026, 10, 9);    // confirmed Friday
        Assert.Equal(DayOfWeek.Wednesday, wednesday.DayOfWeek);
        Assert.Equal(DayOfWeek.Friday, friday.DayOfWeek);

        var wedSlots = schedule.GetAvailableStartTimesOnDate(wednesday, 30, []);
        var friSlots = schedule.GetAvailableStartTimesOnDate(friday, 30, []);

        // Wednesday range starts at 10:00, Friday at 12:00 — they must not mix
        Assert.All(wedSlots, s => Assert.True(s < new TimeOnly(14, 0)));
        Assert.All(friSlots, s => Assert.True(s >= new TimeOnly(12, 0)));
    }

    // ── Slot generation tests ─────────────────────────────────────────────────

    [Fact]
    public void GetAvailableStartTimesOnDate_ReturnsCorrectSlotCount_WhenNoAppointments()
    {
        // Wednesday 10:00–14:00, duration 30 min
        // Last valid start: 13:30 (13:30 + 30 = 14:00). Steps of 5 min → 43 slots.
        var schedule = BuildSchedule();
        var wednesday = new DateOnly(2026, 10, 7);

        var result = schedule.GetAvailableStartTimesOnDate(wednesday, 30, []);

        Assert.Equal(43, result.Count);
        Assert.Equal(new TimeOnly(10, 0), result.First());
        Assert.Equal(new TimeOnly(13, 30), result.Last());
    }

    [Fact]
    public void GetAvailableStartTimesOnDate_SlotsAreInFiveMinuteIncrements()
    {
        var schedule = BuildSchedule();
        var wednesday = new DateOnly(2026, 10, 7);

        var result = schedule.GetAvailableStartTimesOnDate(wednesday, 30, []);

        for (int i = 1; i < result.Count; i++)
        {
            var gap = result[i] - result[i - 1];
            Assert.Equal(TimeSpan.FromMinutes(5), gap);
        }
    }

    // ── Conflict / overlap tests ──────────────────────────────────────────────

    [Fact]
    public void GetAvailableStartTimesOnDate_ExcludesOverlappingSlots_WhenAppointmentExists()
    {
        // Appointment at 10:00 for 30 min blocks slots 10:00–10:25 (6 slots).
        // First clear slot is 10:30.
        var schedule = BuildSchedule();
        var wednesday = new DateOnly(2026, 10, 7);
        var appointment = BuildAgreement(wednesday, new TimeOnly(10, 0), 30);

        var result = schedule.GetAvailableStartTimesOnDate(wednesday, 30, [appointment]);

        Assert.Equal(37, result.Count);
        Assert.DoesNotContain(new TimeOnly(10, 0), result);
        Assert.DoesNotContain(new TimeOnly(10, 25), result);
        Assert.Contains(new TimeOnly(10, 30), result);
    }

    [Fact]
    public void GetAvailableStartTimesOnDate_ReturnsEmpty_WhenAppointmentCoversEntireRange()
    {
        // Appointment at 10:00 for 240 min (4 hours) fills the whole Wednesday range.
        var schedule = BuildSchedule();
        var wednesday = new DateOnly(2026, 10, 7);
        var appointment = BuildAgreement(wednesday, new TimeOnly(10, 0), 240);

        var result = schedule.GetAvailableStartTimesOnDate(wednesday, 30, [appointment]);

        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableStartTimesOnDate_SlotNotBlocked_WhenAppointmentEndsExactlyAtSlotStart()
    {
        // Appointment 10:00–10:30. Slot at 10:30 should NOT be blocked.
        var schedule = BuildSchedule();
        var wednesday = new DateOnly(2026, 10, 7);
        var appointment = BuildAgreement(wednesday, new TimeOnly(10, 0), 30);

        var result = schedule.GetAvailableStartTimesOnDate(wednesday, 30, [appointment]);

        Assert.Contains(new TimeOnly(10, 30), result);
    }
}
