using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Admin;
using OppSignal.Application.Auth;
using OppSignal.Application.Common;
using OppSignal.Application.Email;
using OppSignal.Application.Ingest;
using OppSignal.Infrastructure.Admin;
using OppSignal.Infrastructure.Auth;
using OppSignal.Infrastructure.Common;
using OppSignal.Infrastructure.Email;
using OppSignal.Infrastructure.Identity;
using OppSignal.Infrastructure.Ingest;
using OppSignal.Infrastructure.Persistence;
using OppSignal.Infrastructure.Persistence.Seed;

namespace OppSignal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOppSignalInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ---- Options ----
        services.Configure<BrandingOptions>(config.GetSection(BrandingOptions.SectionName));
        services.Configure<IngestOptions>(config.GetSection(IngestOptions.SectionName));
        services.Configure<SamOptions>(config.GetSection(SamOptions.SectionName));
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<AuthOptions>(config.GetSection(AuthOptions.SectionName));

        // ---- Database ----
        var connectionString = config.GetConnectionString("Postgres")
            ?? config["ConnectionStrings:Postgres"]
            ?? "Host=localhost;Port=5432;Database=oppsignal;Username=oppsignal;Password=oppsignal";
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString, npg =>
            npg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ---- Identity (UserManager only; SPA uses JWTs, not cookies) ----
        services.AddIdentityCore<AppUser>(o =>
        {
            o.User.RequireUniqueEmail = true;
            o.Password.RequiredLength = 10;
            o.Password.RequireDigit = true;
            o.Password.RequireLowercase = true;
            o.Password.RequireUppercase = true;
            o.Password.RequireNonAlphanumeric = false;
            o.SignIn.RequireConfirmedEmail = false; // enforced explicitly in AuthService for a clear message
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ---- Core services ----
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminMetricsService, AdminMetricsService>();

        // ---- Ingest source (Fixture default; Sam when configured) ----
        var ingestSource = config[$"{IngestOptions.SectionName}:Source"] ?? "Fixture";
        if (string.Equals(ingestSource, "Sam", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ISamOpportunitiesClient, SamOpportunitiesClient>();
        }
        else
        {
            services.AddSingleton<ISamOpportunitiesClient, FixtureOpportunitiesClient>();
        }

        // ---- Email transport (Dev default; Postmark when configured) ----
        services.AddSingleton<IEmailRenderer, RazorEmailRenderer>();
        var emailProvider = config[$"{EmailOptions.SectionName}:Provider"] ?? "Dev";
        if (string.Equals(emailProvider, "Postmark", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IEmailSender, PostmarkEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, DevEmailSender>();
        }
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDigestService, DigestService>();

        // ---- Seeders ----
        services.AddScoped<ReferenceSeeder>();
        services.AddScoped<DemoSeeder>();
        services.AddScoped<DbInitializer>();

        return services;
    }
}
