using FluentAssertions;
using OppSignal.Application.Email;
using Xunit;

namespace OppSignal.UnitTests.Email;

public class DeadlineReminderTests
{
    // A fixed, DST-active summer instant so timezone math is unambiguous.
    private static readonly DateTime NowUtc = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(7, 7)]
    [InlineData(3, 3)]
    [InlineData(1, 1)]
    public void Returns_the_threshold_on_a_reminder_day(int daysOut, int expected)
    {
        var deadline = NowUtc.AddDays(daysOut);
        DeadlineReminder.ThresholdFor(deadline, NowUtc, "UTC").Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]   // due today — not a threshold
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(30)]
    public void Returns_null_off_a_reminder_day(int daysOut)
    {
        var deadline = NowUtc.AddDays(daysOut);
        DeadlineReminder.ThresholdFor(deadline, NowUtc, "UTC").Should().BeNull();
    }

    [Fact]
    public void Returns_null_for_a_deadline_already_passed()
    {
        DeadlineReminder.ThresholdFor(NowUtc.AddDays(-1), NowUtc, "UTC").Should().BeNull();
    }

    [Fact]
    public void Same_calendar_day_is_zero_days_regardless_of_time()
    {
        // now 12:00Z, deadline 23:00Z the same UTC day → still "0 days".
        var laterSameDay = new DateTime(2026, 6, 1, 23, 0, 0, DateTimeKind.Utc);
        DeadlineReminder.LocalDaysUntil(laterSameDay, NowUtc, "UTC").Should().Be(0);
    }

    [Fact]
    public void Day_count_is_taken_in_the_users_local_time_not_utc()
    {
        // now = Jun 1 08:00 in New York (EDT, UTC-4); deadline = Jun 7 22:00 local.
        // Local date diff is 6 (Jun 7 − Jun 1), but the UTC dates differ by 7.
        var deadlineUtc = new DateTime(2026, 6, 8, 2, 0, 0, DateTimeKind.Utc); // Jun 7 22:00 EDT

        DeadlineReminder.LocalDaysUntil(deadlineUtc, NowUtc, "America/New_York").Should().Be(6);
        DeadlineReminder.LocalDaysUntil(deadlineUtc, NowUtc, "UTC").Should().Be(7);

        // Because it is 6 local days out, it is NOT on a reminder threshold.
        DeadlineReminder.ThresholdFor(deadlineUtc, NowUtc, "America/New_York").Should().BeNull();
    }

    [Fact]
    public void Unknown_timezone_falls_back_to_utc_without_throwing()
    {
        var deadline = NowUtc.AddDays(7);
        DeadlineReminder.ThresholdFor(deadline, NowUtc, "Not/ARealZone").Should().Be(7);
    }
}
