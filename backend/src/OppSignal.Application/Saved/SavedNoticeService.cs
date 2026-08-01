using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Common;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Saved;

public interface ISavedNoticeService
{
    Task SaveAsync(Guid userId, string noticeId, string? note, CancellationToken ct = default);
    Task UnsaveAsync(Guid userId, string noticeId, CancellationToken ct = default);
    Task<int> CountAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<SavedNoticeDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid userId, string noticeId, PipelineStatus status, CancellationToken ct = default);
}

/// <summary>Star / unstar notices (idempotent), with an optional note.</summary>
public sealed class SavedNoticeService : ISavedNoticeService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public SavedNoticeService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task SaveAsync(Guid userId, string noticeId, string? note, CancellationToken ct = default)
    {
        var exists = await _db.Notices.AnyAsync(n => n.NoticeId == noticeId, ct);
        if (!exists) throw new NotFoundException("Opportunity not found.");

        var saved = await _db.SavedNotices.FirstOrDefaultAsync(s => s.UserId == userId && s.NoticeId == noticeId, ct);
        if (saved is null)
        {
            _db.SavedNotices.Add(new SavedNotice
            {
                UserId = userId, NoticeId = noticeId, Note = Trim(note), SavedAt = _clock.UtcNow,
            });
        }
        else
        {
            saved.Note = Trim(note); // update note on re-save
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnsaveAsync(Guid userId, string noticeId, CancellationToken ct = default)
    {
        var saved = await _db.SavedNotices.FirstOrDefaultAsync(s => s.UserId == userId && s.NoticeId == noticeId, ct);
        if (saved is null) return; // idempotent
        _db.SavedNotices.Remove(saved);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken ct = default)
        => _db.SavedNotices.CountAsync(s => s.UserId == userId, ct);

    public async Task<IReadOnlyList<SavedNoticeDto>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.SavedNotices.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SavedAt)
            .Join(_db.Notices, s => s.NoticeId, n => n.NoticeId, (s, n) => new { s, n })
            .ToListAsync(ct);

        return rows.Select(r => new SavedNoticeDto
        {
            NoticeId = r.n.NoticeId,
            Title = r.n.Title,
            AgencyPath = r.n.AgencyPath,
            TypeLabel = SamMappings.NoticeTypeLabel(r.n.Type),
            NaicsCode = r.n.NaicsCode,
            SetAside = r.n.SetAside,
            SetAsideLabel = SamMappings.SetAsideName(r.n.SetAside),
            PostedDate = r.n.PostedDate,
            ResponseDeadline = r.n.ResponseDeadline,
            UiLink = r.n.UiLink,
            Note = r.s.Note,
            Status = r.s.Status,
            SavedAt = r.s.SavedAt,
            IsActive = r.n.IsActive,
        }).ToList();
    }

    public async Task UpdateStatusAsync(Guid userId, string noticeId, PipelineStatus status, CancellationToken ct = default)
    {
        var saved = await _db.SavedNotices.FirstOrDefaultAsync(s => s.UserId == userId && s.NoticeId == noticeId, ct)
            ?? throw new NotFoundException("Save this opportunity before setting its stage.");
        saved.Status = status;
        await _db.SaveChangesAsync(ct);
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
