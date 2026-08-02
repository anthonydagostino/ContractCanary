using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OppSignal.Api.Infrastructure;
using OppSignal.Api.Validation;
using OppSignal.Application;
using OppSignal.Infrastructure;
using OppSignal.Infrastructure.Auth;
using OppSignal.Infrastructure.Billing;
using OppSignal.Infrastructure.Persistence.Seed;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var services = builder.Services;
    var config = builder.Configuration;

    // ---- Fail fast on a weak/placeholder JWT signing key (outside Development) ----
    JwtKeyGuard.Validate(config["Jwt:SigningKey"], builder.Environment.IsDevelopment());

    // ---- Application + Infrastructure + Billing ----
    services.AddOppSignalApplication();
    services.AddOppSignalInfrastructure(config);
    services.AddOppSignalBilling(config);

    // ---- MVC + JSON ----
    services.AddControllers().AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
    services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    // ---- Auth (JWT bearer) ----
    // Bind the validation parameters lazily from the SAME IOptions<JwtOptions> the
    // token issuer uses, so the signing and validation keys can never diverge.
    services.AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer();
    services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });
    // Authenticated-by-default: any endpoint without an explicit [AllowAnonymous]
    // requires a valid token, so a forgotten [Authorize] can't silently expose data.
    services.AddAuthorization(o =>
        o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

    // ---- CORS (SPA served separately in dev; same-origin behind Caddy in prod) ----
    var corsOrigins = config.GetSection("Cors:Origins").Get<string[]>()
                      ?? new[] { "http://localhost:5173" };
    services.AddCors(o => o.AddPolicy("spa", p => p
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

    // ---- Rate limiting on auth endpoints ----
    var authPermitLimit = config.GetValue<int?>("RateLimit:AuthPermitLimit") ?? 10;
    services.AddRateLimiter(o =>
    {
        o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        o.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
    });

    // ---- Health checks ----
    services.AddHealthChecks()
        .AddDbContextCheck<OppSignal.Infrastructure.Persistence.AppDbContext>("database");

    var app = builder.Build();

    // ---- Initialize DB (migrate + seed) ----
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync();
    }

    // ---- Pipeline ----
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("spa");
    if (!app.Environment.IsEnvironment("Testing")) app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    // Public infrastructure endpoints (the fallback policy would otherwise require auth).
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHealthChecks("/health/ready").AllowAnonymous();
    app.MapGet("/", () => Results.Ok(new { status = "ok", service = "oppsignal-api" })).AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OppSignal API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
