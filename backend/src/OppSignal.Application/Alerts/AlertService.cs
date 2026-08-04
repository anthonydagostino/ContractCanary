using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Alerts;

public sealed class AlertDto
{
    public Guid Id { get; set; }
    public string NoticeId { get; set; } = "";
    public string NoticeTitle { get; set; } = "";
    public AlertType Type { get; set; }
    public string TypeLabel { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool Read { get; set; }
}

public interface IAlertService
{
    Task<IReadOnlyList<AlertDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>Reads a user's change-alert feed and marks it read.</summary>
public sealed class AlertService : IAlertService
{
    private const int MaxFeed = 100;
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AlertService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AlertDto>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        // The OrderBy before Take becomes a LIMIT subquery; the outer join's
        // result order is unspecified without its own ORDER BY, so sort again
        // after the join (with Id as a stable tiebreak for equal timestamps).
        var rows = await _db.NoticeAlerts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(MaxFeed)
            .Join(_db.Notices, a => a.NoticeId, n => n.NoticeId, (a, n) => new { a, n.Title })
            .OrderByDescending(x => x.a.CreatedAt)
            .ThenByDescending(x => x.a.Id)
            .ToListAsync(ct);

        return rows.Select(r => new AlertDto
        {
            Id = r.a.Id,
            NoticeId = r.a.NoticeId,
            NoticeTitle = r.Title,
            Type = r.a.Type,
            TypeLabel = Label(r.a.Type),
            Message = r.a.Message,
            CreatedAt = r.a.CreatedAt,
            Read = r.a.ReadAt != null,
        }).ToList();
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default)
        => _db.NoticeAlerts.CountAsync(a => a.UserId == userId && a.ReadAt == null, ct);

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.NoticeAlerts
            .Where(a => a.UserId == userId && a.ReadAt == null)
            .ToListAsync(ct);
        if (unread.Count == 0) return;

        var now = _clock.UtcNow;
        foreach (var alert in unread) alert.ReadAt = now;
        await _db.SaveChangesAsync(ct);
    }

    private static string Label(AlertType type) => type switch
    {
        AlertType.DeadlineChanged => "Deadline changed",
        AlertType.Cancelled => "Cancelled",
        _ => "Updated",
    };
}
