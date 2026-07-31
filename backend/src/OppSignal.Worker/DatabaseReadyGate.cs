using Microsoft.EntityFrameworkCore;
using OppSignal.Infrastructure.Persistence;

namespace OppSignal.Worker;

/// <summary>
/// Blocks worker startup until the database is reachable and migrated (the API
/// owns migrations/seed). Ensures the first scheduled ingest doesn't run against
/// an un-provisioned schema in compose.
/// </summary>
public sealed class DatabaseReadyGate : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DatabaseReadyGate> _log;

    public DatabaseReadyGate(IServiceScopeFactory scopes, ILogger<DatabaseReadyGate> log)
    {
        _scopes = scopes;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 60; attempt++)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (await db.Database.CanConnectAsync(cancellationToken))
                {
                    var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
                    if (applied.Any())
                    {
                        _log.LogInformation("Database ready after {Attempt} attempt(s).", attempt);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning("Waiting for database (attempt {Attempt}): {Message}", attempt, ex.Message);
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempt, 5)), cancellationToken);
        }
        _log.LogWarning("Proceeding without confirmed database readiness.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
