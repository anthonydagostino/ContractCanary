using Microsoft.EntityFrameworkCore;
using Npgsql;
using OppSignal.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace OppSignal.IntegrationTests.Support;

/// <summary>
/// Provisions an isolated, migrated Postgres database for a test class.
///
/// By default it spins a disposable <c>postgres:16-alpine</c> Testcontainer (the
/// intended mechanism for the user's Docker-enabled environment). If the
/// <c>TEST_POSTGRES_ADMIN</c> env var is set (a maintenance connection string),
/// it instead creates a fresh uniquely-named database on that server — this lets
/// the suite run in environments where pulling the Docker image is blocked.
/// </summary>
public sealed class PostgresTestDatabase : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _adminConnection = default!;
    private string _dbName = default!;

    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var envAdmin = Environment.GetEnvironmentVariable("TEST_POSTGRES_ADMIN");
        if (!string.IsNullOrWhiteSpace(envAdmin))
        {
            _adminConnection = envAdmin;
        }
        else
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();
            await _container.StartAsync();
            _adminConnection = _container.GetConnectionString();
        }

        _dbName = "opps_test_" + Guid.NewGuid().ToString("N")[..12];

        await using (var admin = new NpgsqlConnection(_adminConnection))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{_dbName}\";";
            await cmd.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = _dbName }.ConnectionString;

        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);
        await db.Database.MigrateAsync();
    }

    public AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    /// <summary>Truncate mutable domain tables so each test starts clean (reference/seed tables untouched).</summary>
    public async Task ResetAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE notice_matches, saved_notices, notices, match_profiles, ingest_runs, email_logs, subscriptions RESTART IDENTITY CASCADE;");
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        // env mode: drop the temp database
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(_adminConnection);
        await admin.OpenAsync();
        await using var cmd = admin.CreateCommand();
        cmd.CommandText =
            $"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE);";
        await cmd.ExecuteNonQueryAsync();
    }
}
