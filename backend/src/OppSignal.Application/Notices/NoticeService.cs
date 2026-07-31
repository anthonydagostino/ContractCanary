using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Common;
using OppSignal.Domain.Entities;

namespace OppSignal.Application.Notices;

public interface INoticeService
{
    Task<PagedResult<NoticeListItemDto>> SearchAsync(Guid userId, NoticeQuery query, CancellationToken ct = default);
    Task<NoticeDetailDto> GetDetailAsync(Guid userId, string noticeId, CancellationToken ct = default);
    Task<string> ExportCsvAsync(Guid userId, NoticeQuery query, CancellationToken ct = default);
}

/// <summary>
/// Server-side opportunity search: filtering, sorting, and pagination over notices,
/// enriched with the caller's saved/matched flags. Also produces CSV export (Pro).
/// </summary>
public sealed class NoticeService : INoticeService
{
    private const int MaxPageSize = 100;
    private const int CsvRowCap = 10_000;
    private readonly IAppDbContext _db;

    public NoticeService(IAppDbContext db) => _db = db;

    public async Task<PagedResult<NoticeListItemDto>> SearchAsync(Guid userId, NoticeQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, MaxPageSize);

        var query = BuildQuery(userId, q);
        var total = await query.CountAsync(ct);

        var notices = await Sort(query, q)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var items = await ProjectAsync(userId, notices, ct);

        return new PagedResult<NoticeListItemDto>
        {
            Items = items, Page = page, PageSize = pageSize, Total = total,
        };
    }

    public async Task<NoticeDetailDto> GetDetailAsync(Guid userId, string noticeId, CancellationToken ct = default)
    {
        var notice = await _db.Notices.AsNoTracking().FirstOrDefaultAsync(n => n.NoticeId == noticeId, ct)
                     ?? throw new NotFoundException("Opportunity not found.");

        var saved = await _db.SavedNotices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.NoticeId == noticeId, ct);

        var matches = await _db.NoticeMatches.AsNoTracking()
            .Where(m => m.UserId == userId && m.NoticeId == noticeId)
            .Join(_db.MatchProfiles, m => m.MatchProfileId, p => p.Id, (m, p) => new { p.Id, p.Name, m.MatchReason })
            .ToListAsync(ct);

        var dto = new NoticeDetailDto();
        MapListItem(dto, notice, saved is not null, matches.Count > 0);
        dto.BaseType = notice.BaseType;
        dto.SubTierName = notice.SubTierName;
        dto.OfficeName = notice.OfficeName;
        dto.SetAsideDescription = notice.SetAsideDescription ?? SamMappings.SetAsideDescription(notice.SetAside);
        dto.ArchiveDate = notice.ArchiveDate;
        dto.PopZip = notice.PopZip;
        dto.PopCountry = notice.PopCountry;
        dto.Description = notice.Description;
        dto.DescriptionLink = notice.DescriptionLink;
        dto.PrimaryContactName = notice.PrimaryContactName;
        dto.PrimaryContactEmail = notice.PrimaryContactEmail;
        dto.PrimaryContactPhone = notice.PrimaryContactPhone;
        dto.IsActive = notice.IsActive;
        dto.SavedNote = saved?.Note;
        dto.MatchedProfiles = matches.Select(m => new MatchedProfileDto(m.Id, m.Name, m.MatchReason)).ToList();
        return dto;
    }

    public async Task<string> ExportCsvAsync(Guid userId, NoticeQuery q, CancellationToken ct = default)
    {
        var query = BuildQuery(userId, q);
        var notices = await Sort(query, q).Take(CsvRowCap).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("NoticeId,Title,SolicitationNumber,Type,Agency,NAICS,PSC,SetAside,Posted,ResponseDeadline,State,City,Link");
        foreach (var n in notices)
        {
            sb.Append(Csv(n.NoticeId)).Append(',')
              .Append(Csv(n.Title)).Append(',')
              .Append(Csv(n.SolicitationNumber)).Append(',')
              .Append(Csv(SamMappings.NoticeTypeLabel(n.Type))).Append(',')
              .Append(Csv(n.AgencyPath)).Append(',')
              .Append(Csv(n.NaicsCode)).Append(',')
              .Append(Csv(n.PscCode)).Append(',')
              .Append(Csv(SamMappings.SetAsideName(n.SetAside))).Append(',')
              .Append(Csv(n.PostedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Append(',')
              .Append(Csv(n.ResponseDeadline?.ToString("u", CultureInfo.InvariantCulture))).Append(',')
              .Append(Csv(n.PopState)).Append(',')
              .Append(Csv(n.PopCity)).Append(',')
              .Append(Csv(n.UiLink)).Append('\n');
        }
        return sb.ToString();
    }

    // ---- query building -----------------------------------------------------

    private IQueryable<Notice> BuildQuery(Guid userId, NoticeQuery q)
    {
        var query = _db.Notices.AsNoTracking().AsQueryable();

        if (q.ActiveOnly is true) query = query.Where(n => n.IsActive);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(term) ||
                (n.Description != null && n.Description.ToLower().Contains(term)) ||
                (n.SolicitationNumber != null && n.SolicitationNumber.ToLower().Contains(term)));
        }

        if (q.Naics.Count > 0) query = query.Where(n => n.NaicsCode != null && q.Naics.Contains(n.NaicsCode));
        if (q.Psc.Count > 0) query = query.Where(n => n.PscCode != null && q.Psc.Contains(n.PscCode));
        if (q.SetAsides.Count > 0) query = query.Where(n => q.SetAsides.Contains(n.SetAside));
        if (q.States.Count > 0) query = query.Where(n => n.PopState != null && q.States.Contains(n.PopState));
        if (q.NoticeTypes.Count > 0) query = query.Where(n => q.NoticeTypes.Contains(n.Type));
        if (!string.IsNullOrWhiteSpace(q.Agency))
        {
            var term = q.Agency.Trim().ToLower();
            query = query.Where(n => n.AgencyPath != null && n.AgencyPath.ToLower().Contains(term));
        }
        if (q.PostedFrom is { } pf) query = query.Where(n => n.PostedDate >= pf);
        if (q.PostedTo is { } pt) query = query.Where(n => n.PostedDate <= pt);

        if (q.ProfileId is { } pid)
            query = query.Where(n => _db.NoticeMatches.Any(m => m.NoticeId == n.NoticeId && m.MatchProfileId == pid && m.UserId == userId));
        else if (q.MatchedOnly)
            query = query.Where(n => _db.NoticeMatches.Any(m => m.NoticeId == n.NoticeId && m.UserId == userId));

        if (q.SavedOnly)
            query = query.Where(n => _db.SavedNotices.Any(s => s.NoticeId == n.NoticeId && s.UserId == userId));

        return query;
    }

    private static IQueryable<Notice> Sort(IQueryable<Notice> query, NoticeQuery q)
    {
        var asc = string.Equals(q.Direction, "asc", StringComparison.OrdinalIgnoreCase);
        return q.Sort?.ToLowerInvariant() switch
        {
            "deadline" => asc
                ? query.OrderBy(n => n.ResponseDeadline == null).ThenBy(n => n.ResponseDeadline).ThenByDescending(n => n.PostedDate)
                : query.OrderBy(n => n.ResponseDeadline == null).ThenByDescending(n => n.ResponseDeadline).ThenByDescending(n => n.PostedDate),
            _ => asc
                ? query.OrderBy(n => n.PostedDate).ThenBy(n => n.NoticeId)
                : query.OrderByDescending(n => n.PostedDate).ThenBy(n => n.NoticeId),
        };
    }

    private async Task<List<NoticeListItemDto>> ProjectAsync(Guid userId, List<Notice> notices, CancellationToken ct)
    {
        var ids = notices.Select(n => n.NoticeId).ToList();
        var saved = (await _db.SavedNotices.AsNoTracking()
            .Where(s => s.UserId == userId && ids.Contains(s.NoticeId))
            .Select(s => s.NoticeId).ToListAsync(ct)).ToHashSet();
        var matched = (await _db.NoticeMatches.AsNoTracking()
            .Where(m => m.UserId == userId && ids.Contains(m.NoticeId))
            .Select(m => m.NoticeId).Distinct().ToListAsync(ct)).ToHashSet();

        return notices.Select(n =>
        {
            var dto = new NoticeListItemDto();
            MapListItem(dto, n, saved.Contains(n.NoticeId), matched.Contains(n.NoticeId));
            return dto;
        }).ToList();
    }

    private static void MapListItem(NoticeListItemDto dto, Notice n, bool isSaved, bool isMatched)
    {
        dto.NoticeId = n.NoticeId;
        dto.Title = n.Title;
        dto.SolicitationNumber = n.SolicitationNumber;
        dto.Type = n.Type;
        dto.TypeLabel = SamMappings.NoticeTypeLabel(n.Type);
        dto.AgencyPath = n.AgencyPath;
        dto.DepartmentName = n.DepartmentName;
        dto.NaicsCode = n.NaicsCode;
        dto.PscCode = n.PscCode;
        dto.SetAside = n.SetAside;
        dto.SetAsideLabel = SamMappings.SetAsideName(n.SetAside);
        dto.PostedDate = n.PostedDate;
        dto.ResponseDeadline = n.ResponseDeadline;
        dto.PopState = n.PopState;
        dto.PopCity = n.PopCity;
        dto.UiLink = n.UiLink;
        dto.IsSaved = isSaved;
        dto.IsMatched = isMatched;
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var needsQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuote ? $"\"{escaped}\"" : escaped;
    }
}
