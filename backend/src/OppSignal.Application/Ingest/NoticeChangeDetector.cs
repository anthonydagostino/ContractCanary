using System.Globalization;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Ingest;

public sealed record DetectedChange(AlertType Type, string Message);

/// <summary>
/// Compares a notice's prior stored state to the freshly-ingested version and
/// reports the material changes worth alerting a tracking user about: the response
/// deadline moved, or the opportunity was cancelled/archived. Pure and unit-tested.
/// Deliberately conservative — trivial field churn (which SAM re-publishes often)
/// produces no alert, to avoid noise.
/// </summary>
public static class NoticeChangeDetector
{
    public static IReadOnlyList<DetectedChange> Detect(Notice before, Notice after)
    {
        var changes = new List<DetectedChange>();

        // Cancellation supersedes everything else — no point alerting a moved
        // deadline on something that's no longer open.
        if (before.IsActive && !after.IsActive)
        {
            changes.Add(new DetectedChange(
                AlertType.Cancelled, "This opportunity was cancelled or archived on SAM.gov."));
            return changes;
        }

        // Deadline moved to a real new date, and still open.
        if (after.IsActive && after.ResponseDeadline is { } nd &&
            before.ResponseDeadline != after.ResponseDeadline)
        {
            var newStr = nd.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
            var message = before.ResponseDeadline is { } od
                ? $"Response deadline moved from {od.ToString("MMM d", CultureInfo.InvariantCulture)} to {newStr}."
                : $"Response deadline set to {newStr}.";
            changes.Add(new DetectedChange(AlertType.DeadlineChanged, message));
        }

        return changes;
    }
}
