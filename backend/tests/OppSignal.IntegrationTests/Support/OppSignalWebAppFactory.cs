using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace OppSignal.IntegrationTests.Support;

/// <summary>
/// Boots the real API against an isolated Postgres (see <see cref="PostgresTestDatabase"/>),
/// in Fixture ingest mode with the demo seed disabled and deterministic test
/// config (Stripe webhook secret + price ids, JWT key). Reference data is seeded
/// by the app's own initializer.
/// </summary>
public sealed class OppSignalWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public PostgresTestDatabase Db { get; } = new();

    public const string WebhookSecret = "whsec_test_secret_for_integration";
    public const string StarterPriceId = "price_starter_test";
    public const string ProPriceId = "price_pro_test";

    public async Task InitializeAsync() => await Db.InitializeAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Db.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = Db.ConnectionString,
                ["Ingest:Source"] = "Fixture",
                ["Seed:Demo"] = "false",
                ["RateLimit:AuthPermitLimit"] = "100000", // don't throttle the test suite
                ["Auth:RequireConfirmedEmail"] = "true",
                ["Jwt:SigningKey"] = "integration-test-signing-key-at-least-32-bytes!!",
                ["Email:Provider"] = "Dev",
                ["Email:DevDropPath"] = Path.Combine(Path.GetTempPath(), "oppsignal-test-mail"),
                ["Stripe:SecretKey"] = "sk_test_dummy",
                ["Stripe:WebhookSecret"] = WebhookSecret,
                ["Stripe:StarterPriceId"] = StarterPriceId,
                ["Stripe:ProPriceId"] = ProPriceId,
            });
        });
    }
}
