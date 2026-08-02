using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OppSignal.Application.Ingest;

namespace OppSignal.Infrastructure.Persistence.Seed;

/// <summary>
/// Startup initialization: apply migrations, seed reference data, and (in fixture
/// mode, unless disabled) seed the demo. Retries connecting so it survives a
/// still-starting Postgres in compose.
/// </summary>
public sealed class DbInitializer
{
    private readonly AppDbContext _db;
    private readonly ReferenceSeeder _reference;
    private readonly DemoSeeder _demo;
    private readonly AccountSafetyGuard _accountSafety;
    private readonly IConfiguration _config;
    private readonly ILogger<DbInitializer> _log;

    public DbInitializer(
        AppDbContext db, ReferenceSeeder reference, DemoSeeder demo, AccountSafetyGuard accountSafety,
        IConfiguration config, ILogger<DbInitializer> log)
    {
        _db = db;
        _reference = reference;
        _demo = demo;
        _accountSafety = accountSafety;
        _config = config;
        _log = log;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await WaitForDatabaseAsync(ct);

        _log.LogInformation("Applying migrations…");
        await _db.Database.MigrateAsync(ct);

        _log.LogInformation("Seeding reference data…");
        await _reference.SeedAsync(ct);

        var ingestSource = _config["Ingest:Source"] ?? "Fixture";
        var demoEnabled = _config.GetValue<bool?>("Seed:Demo")
            ?? string.Equals(ingestSource, "Fixture", StringComparison.OrdinalIgnoreCase);

        if (demoEnabled)
        {
            _log.LogInformation("Seeding demo data…");
            await _demo.SeedAsync(ct);
        }

        // Promote configured admins, and (in real deployments) lock any lingering
        // default demo/admin accounts that still use their shipped password.
        await _accountSafety.RunAsync(demoEnabled, ct);

        _log.LogInformation("Initialization complete.");
    }

    private async Task WaitForDatabaseAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= 20; attempt++)
        {
            try
            {
                if (await _db.Database.CanConnectAsync(ct)) return;
            }
            catch (Exception ex) when (attempt < 20)
            {
                _log.LogWarning("Database not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempt, 5)), ct);
        }
    }
}
