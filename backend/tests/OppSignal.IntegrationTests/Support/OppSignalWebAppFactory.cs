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

        // IMPORTANT: these must be UseSetting, not ConfigureAppConfiguration/
        // AddInMemoryCollection. With the minimal hosting model, top-level code in
        // Program.cs reads builder.Configuration DURING composition (JwtKeyGuard,
        // the connection string), and values added via ConfigureAppConfiguration
        // are not visible at that point — the app then boots with the blank
        // committed signing key and JwtKeyGuard aborts startup ("entry point
        // exited without ever building an IHost"). UseSetting flows into host
        // configuration, which IS visible to those reads.
        builder.UseSetting("ConnectionStrings:Postgres", Db.ConnectionString);
        builder.UseSetting("Ingest:Source", "Fixture");
        builder.UseSetting("Seed:Demo", "false");
        builder.UseSetting("RateLimit:AuthPermitLimit", "100000"); // don't throttle the test suite
        builder.UseSetting("Auth:RequireConfirmedEmail", "true");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-bytes!!");
        builder.UseSetting("Email:Provider", "Dev");
        builder.UseSetting("Email:DevDropPath", Path.Combine(Path.GetTempPath(), "oppsignal-test-mail"));
        builder.UseSetting("Stripe:SecretKey", "sk_test_dummy");
        builder.UseSetting("Stripe:WebhookSecret", WebhookSecret);
        builder.UseSetting("Stripe:StarterPriceId", StarterPriceId);
        builder.UseSetting("Stripe:ProPriceId", ProPriceId);
    }
}
