using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Common;

namespace OppSignal.Application.Reference;

public sealed record NaicsDto(string Code, string Title, int Level);
public sealed record PscDto(string Code, string Title, string Category, bool IsService);
public sealed record AgencyDto(string Code, string Name, int Tier, string? ParentCode);
public sealed record SetAsideDto(string Code, string Name, string Description);
public sealed record NoticeTypeDto(string Value, string Label);

public interface IReferenceService
{
    Task<IReadOnlyList<NaicsDto>> SearchNaicsAsync(string? term, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<PscDto>> SearchPscAsync(string? term, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<AgencyDto>> SearchAgenciesAsync(string? term, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<SetAsideDto>> GetSetAsidesAsync(CancellationToken ct = default);
    IReadOnlyList<NoticeTypeDto> GetNoticeTypes();
}

/// <summary>Read-only reference lookups powering the profile-editor typeaheads.</summary>
public sealed class ReferenceService : IReferenceService
{
    private readonly IAppDbContext _db;
    public ReferenceService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<NaicsDto>> SearchNaicsAsync(string? term, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var query = _db.NaicsCodes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            var lower = t.ToLower();
            query = query.Where(n => n.Code.StartsWith(t) || n.Title.ToLower().Contains(lower));
        }
        else
        {
            query = query.Where(n => n.Level == 2); // default: show sectors
        }
        return await query.OrderBy(n => n.Code).Take(limit)
            .Select(n => new NaicsDto(n.Code, n.Title, n.Level)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PscDto>> SearchPscAsync(string? term, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var query = _db.PscCodes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            var lower = t.ToLower();
            query = query.Where(p => p.Code.StartsWith(t) || p.Title.ToLower().Contains(lower));
        }
        return await query.OrderBy(p => p.Code).Take(limit)
            .Select(p => new PscDto(p.Code, p.Title, p.Category, p.IsService)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AgencyDto>> SearchAgenciesAsync(string? term, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 60);
        var query = _db.Agencies.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var lower = term.Trim().ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(lower));
        }
        return await query.OrderBy(a => a.Tier).ThenBy(a => a.Name).Take(limit)
            .Select(a => new AgencyDto(a.Code, a.Name, a.Tier, a.ParentCode)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SetAsideDto>> GetSetAsidesAsync(CancellationToken ct = default)
        => await _db.SetAsides.AsNoTracking().OrderBy(s => s.Name)
            .Select(s => new SetAsideDto(s.Code, s.Name, s.Description)).ToListAsync(ct);

    public IReadOnlyList<NoticeTypeDto> GetNoticeTypes() => SamMappings.ProfileSelectableNoticeTypes
        .Select(t => new NoticeTypeDto(t.ToString(), SamMappings.NoticeTypeLabel(t))).ToList();
}
