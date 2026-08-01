using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Billing;
using OppSignal.Application.Common;
using OppSignal.Application.Email;
using OppSignal.Domain.Entities;
using OppSignal.Infrastructure.Persistence;

namespace OppSignal.Infrastructure.Email;

public interface IDigestService
{
    /// <summary>Send digests to every eligible user whose local time is at the send hour. Returns count sent.</summary>
    Task<int> SendDueDigestsAsync(int sendHourLocal, CancellationToken ct = default);

    /// <summary>Send a single user's digest immediately (used for demo/dev/manual trigger). Returns true if sent.</summary>
    Task<bool> SendUserDigestAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Builds and sends the daily digest: groups a user's un-notified matches by
/// profile, renders in the user's timezone, and stamps <c>NotifiedAt</c> so a
/// match is never emailed twice. Zero matches → no email (enforced downstream).
/// </summary>
public sealed class DigestService : IDigestService
{
    private const int MaxItemsPerProfile = 20;
    private const int MaxAlerts = 20;
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IEntitlementService _entitlements;
    private readonly IClock _clock;
    private readonly BrandingOptions _branding;
    private readonly ILogger<DigestService> _log;

    public DigestService(
        AppDbContext db,
        INotificationService notifications,
        IEntitlementService entitlements,
        IClock clock,
        IOptions<BrandingOptions> branding,
        ILogger<DigestService> log)
    {
        _db = db;
        _notifications = notifications;
        _entitlements = entitlements;
        _clock = clock;
        _branding = branding.Value;
        _log = log;
    }

    public async Task<int> SendDueDigestsAsync(int sendHourLocal, CancellationToken ct = default)
    {
        // A user is due if they have un-notified matches OR un-notified change-alerts.
        var matchUserIds = await _db.NoticeMatches
            .Where(m => m.NotifiedAt == null)
            .Select(m => m.UserId).Distinct().ToListAsync(ct);
        var alertUserIds = await _db.NoticeAlerts
            .Where(a => a.NotifiedAt == null)
            .Select(a => a.UserId).Distinct().ToListAsync(ct);
        var userIds = matchUserIds.Union(alertUserIds).Distinct().ToList();

        var sent = 0;
        foreach (var userId in userIds)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null || !user.EmailConfirmed) continue;

            var localNow = ToLocal(_clock.UtcNow, user.TimeZoneId);
            if (localNow.Hour != sendHourLocal) continue;

            if (await SendUserDigestAsync(userId, ct)) sent++;
        }

        if (sent > 0) _log.LogInformation("Sent {Count} daily digests (hour {Hour})", sent, sendHourLocal);
        return sent;
    }

    public async Task<bool> SendUserDigestAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return false;

        var entitlement = await _entitlements.GetAsync(userId, ct);
        if (!entitlement.Limits.DailyDigest) return false; // plan does not include digest (e.g. expired trial)

        var rows = await _db.NoticeMatches
            .Where(m => m.UserId == userId && m.NotifiedAt == null)
            .Join(_db.Notices, m => m.NoticeId, n => n.NoticeId, (m, n) => new { m, n })
            .Join(_db.MatchProfiles.Where(p => p.IsActive), x => x.m.MatchProfileId, p => p.Id, (x, p) => new { x.m, x.n, p })
            .OrderByDescending(x => x.n.PostedDate)
            .ToListAsync(ct);

        var alertRows = await _db.NoticeAlerts
            .Where(a => a.UserId == userId && a.NotifiedAt == null)
            .Join(_db.Notices, a => a.NoticeId, n => n.NoticeId, (a, n) => new { a, n.Title })
            .OrderByDescending(x => x.a.CreatedAt)
            .ToListAsync(ct);

        if (rows.Count == 0 && alertRows.Count == 0) return false;

        var localNow = ToLocal(_clock.UtcNow, user.TimeZoneId);
        var model = new DigestModel
        {
            UserId = userId,
            ToEmail = user.Email!,
            ToName = user.FullName,
            DateLabel = localNow.ToString("dddd, MMMM d"),
            WebBaseUrl = _branding.WebBaseUrl,
        };

        foreach (var x in alertRows.Take(MaxAlerts))
            model.Alerts.Add(ToAlert(x.a, x.Title, _branding.WebBaseUrl));

        foreach (var g in rows.GroupBy(x => new { x.p.Id, x.p.Name }).OrderBy(g => g.Key.Name))
        {
            var group = new DigestGroup { ProfileId = g.Key.Id, ProfileName = g.Key.Name };
            foreach (var x in g.Take(MaxItemsPerProfile))
                group.Items.Add(ToItem(x.n, user.TimeZoneId, _branding.WebBaseUrl));
            model.Groups.Add(group);
        }

        var sentOk = await _notifications.SendDigestAsync(model, ct);

        // Mark ALL of the user's currently un-notified matches AND change-alerts as
        // notified so nothing is emailed twice (backlog beyond the caps stays
        // browsable in-app).
        if (sentOk)
        {
            var now = _clock.UtcNow;
            var matchesToStamp = await _db.NoticeMatches.Where(m => m.UserId == userId && m.NotifiedAt == null).ToListAsync(ct);
            foreach (var m in matchesToStamp) m.NotifiedAt = now;
            var alertsToStamp = await _db.NoticeAlerts.Where(a => a.UserId == userId && a.NotifiedAt == null).ToListAsync(ct);
            foreach (var a in alertsToStamp) a.NotifiedAt = now;
            await _db.SaveChangesAsync(ct);
        }

        return sentOk;
    }

    private static DigestAlert ToAlert(NoticeAlert a, string title, string webBaseUrl) => new()
    {
        NoticeId = a.NoticeId,
        Title = title,
        TypeLabel = a.Type == Domain.Enums.AlertType.Cancelled ? "Cancelled" : "Deadline changed",
        Message = a.Message,
        IsCancelled = a.Type == Domain.Enums.AlertType.Cancelled,
        DetailLink = $"{webBaseUrl.TrimEnd('/')}/app/opportunities/{a.NoticeId}",
    };

    private static DigestItem ToItem(Notice n, string tz, string webBaseUrl)
    {
        var place = new[] { n.PopCity, n.PopState }.Where(s => !string.IsNullOrWhiteSpace(s));
        return new DigestItem
        {
            NoticeId = n.NoticeId,
            Title = n.Title,
            Agency = n.AgencyPath ?? n.DepartmentName ?? "",
            TypeLabel = OppSignal.Application.Common.SamMappings.NoticeTypeLabel(n.Type),
            Deadline = n.ResponseDeadline is { } d ? ToLocal(d, tz).ToString("ddd, MMM d, yyyy · h:mm tt") : null,
            SetAside = n.SetAside == Domain.Enums.SetAsideCode.None ? null : OppSignal.Application.Common.SamMappings.SetAsideName(n.SetAside),
            PlaceOfPerformance = place.Any() ? string.Join(", ", place) : null,
            SamLink = n.UiLink ?? $"https://sam.gov/opp/{n.NoticeId}/view",
            // DetailLink base is fixed up with the real WebBaseUrl by the notification layer.
            DetailLink = $"{webBaseUrl.TrimEnd('/')}/app/opportunities/{n.NoticeId}",
        };
    }

    private static DateTime ToLocal(DateTime utc, string tz)
    {
        try
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tzInfo);
        }
        catch
        {
            return utc;
        }
    }
}
