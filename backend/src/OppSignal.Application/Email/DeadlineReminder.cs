namespace OppSignal.Application.Email;

/// <summary>
/// Pure deadline-reminder threshold logic for the daily digest. An opportunity a
/// user is tracking (matched or saved) is re-surfaced when its response deadline
/// is exactly 7, 3, or 1 day away in the user's local time — at most three gentle
/// nudges over its life, with no "already reminded" bookkeeping needed. Because
/// the digest fires once per calendar day per user, each threshold can hit only
/// once. Deterministic and timezone-aware, so it is exhaustively unit-tested and
/// carries the correctness guarantee independently of any database or clock.
/// </summary>
public static class DeadlineReminder
{
    /// <summary>Day-counts at which a tracked opportunity is re-surfaced (soonest last).</summary>
    public static readonly IReadOnlyList<int> Thresholds = new[] { 7, 3, 1 };

    /// <summary>
    /// If <paramref name="deadlineUtc"/> falls on one of the reminder thresholds
    /// (7 / 3 / 1 days) relative to <paramref name="nowUtc"/> in
    /// <paramref name="timeZoneId"/>, returns that threshold; otherwise null.
    /// </summary>
    public static int? ThresholdFor(DateTime deadlineUtc, DateTime nowUtc, string timeZoneId)
    {
        var days = LocalDaysUntil(deadlineUtc, nowUtc, timeZoneId);
        return Thresholds.Contains(days) ? days : null;
    }

    /// <summary>
    /// Whole calendar days from "today" to the deadline's day, both taken in
    /// <paramref name="timeZoneId"/>. Same day → 0; tomorrow → 1; already past → negative.
    /// </summary>
    public static int LocalDaysUntil(DateTime deadlineUtc, DateTime nowUtc, string timeZoneId)
    {
        var tz = ResolveTz(timeZoneId);
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz);
        var deadlineLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(deadlineUtc, DateTimeKind.Utc), tz);
        return (deadlineLocal.Date - nowLocal.Date).Days;
    }

    private static TimeZoneInfo ResolveTz(string timeZoneId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return TimeZoneInfo.Utc; }
    }
}
