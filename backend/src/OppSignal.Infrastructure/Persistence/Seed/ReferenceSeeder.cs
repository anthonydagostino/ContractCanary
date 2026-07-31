using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OppSignal.Application.Common;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotently seeds the reference tables (NAICS, PSC, Agencies, Set-asides)
/// from embedded JSON. Safe to run on every startup — each table is only
/// populated when empty.
/// </summary>
public class ReferenceSeeder
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;
    private readonly ILogger<ReferenceSeeder> _log;

    public ReferenceSeeder(AppDbContext db, ILogger<ReferenceSeeder> log)
    {
        _db = db;
        _log = log;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedNaicsAsync(ct);
        await SeedPscAsync(ct);
        await SeedAgenciesAsync(ct);
        await SeedSetAsidesAsync(ct);
    }

    private async Task SeedNaicsAsync(CancellationToken ct)
    {
        if (await _db.NaicsCodes.AnyAsync(ct)) return;
        var items = JsonSerializer.Deserialize<List<NaicsSeed>>(EmbeddedResources.ReadText("naics.json"), Json)!;
        _db.NaicsCodes.AddRange(items.Select(i => new NaicsCode
        {
            Code = i.Code, Title = i.Title, Level = i.Level, ParentCode = i.ParentCode,
        }));
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Seeded {Count} NAICS codes", items.Count);
    }

    private async Task SeedPscAsync(CancellationToken ct)
    {
        if (await _db.PscCodes.AnyAsync(ct)) return;
        var items = JsonSerializer.Deserialize<List<PscSeed>>(EmbeddedResources.ReadText("psc.json"), Json)!;
        _db.PscCodes.AddRange(items.Select(i => new PscCode
        {
            Code = i.Code, Title = i.Title, Category = i.Category, IsService = i.IsService,
        }));
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Seeded {Count} PSC codes", items.Count);
    }

    private async Task SeedAgenciesAsync(CancellationToken ct)
    {
        if (await _db.Agencies.AnyAsync(ct)) return;
        var items = JsonSerializer.Deserialize<List<AgencySeed>>(EmbeddedResources.ReadText("agencies.json"), Json)!;
        _db.Agencies.AddRange(items.Select(i => new Agency
        {
            Code = i.Code, Name = i.Name, Tier = i.Tier, ParentCode = i.ParentCode,
        }));
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Seeded {Count} agencies", items.Count);
    }

    private async Task SeedSetAsidesAsync(CancellationToken ct)
    {
        if (await _db.SetAsides.AnyAsync(ct)) return;
        var rows = SamMappings.SetAsideToCode
            .Select(kv => new SetAsideRef
            {
                Code = kv.Value,
                Name = SamMappings.SetAsideName(kv.Key),
                Description = SamMappings.SetAsideDescription(kv.Key),
            })
            .GroupBy(r => r.Code).Select(g => g.First())
            .ToList();
        _db.SetAsides.AddRange(rows);
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Seeded {Count} set-aside codes", rows.Count);
    }

    private record NaicsSeed(string Code, string Title, int Level, string? ParentCode);
    private record PscSeed(string Code, string Title, string Category, bool IsService);
    private record AgencySeed(string Code, string Name, int Tier, string? ParentCode);
}
