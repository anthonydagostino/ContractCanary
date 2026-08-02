using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Ingest;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Identity;

namespace OppSignal.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a fully clickable demo (fixture mode): a Pro demo user + an admin user,
/// three match profiles, and a wide bootstrap ingest so the dashboard is
/// populated and the first digest has content. Idempotent — skips if the demo
/// user already exists. Login is documented in the README.
/// </summary>
public sealed class DemoSeeder
{
    private readonly UserManager<AppUser> _users;
    private readonly AppDbContext _db;
    private readonly IIngestService _ingest;
    private readonly IClock _clock;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DemoSeeder> _log;

    public DemoSeeder(
        UserManager<AppUser> users, AppDbContext db, IIngestService ingest,
        IClock clock, IConfiguration config, IHostEnvironment env, ILogger<DemoSeeder> log)
    {
        _users = users;
        _db = db;
        _ingest = ingest;
        _clock = clock;
        _config = config;
        _env = env;
        _log = log;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var isDev = _env.IsDevelopment();
        var demoEmail = (_config["Demo:Email"] ?? "demo@oppsignal.dev").ToLowerInvariant();
        var adminEmail = (_config["Demo:AdminEmail"] ?? "admin@oppsignal.dev").ToLowerInvariant();
        // Outside Development, a real password MUST be configured — never fall back to a shipped default.
        var demoPassword = _config["Demo:Password"] ?? (isDev ? "DemoPassword123!" : null);
        var adminPassword = _config["Demo:AdminPassword"] ?? (isDev ? "AdminPassword123!" : null);

        if (demoPassword is null)
        {
            _log.LogWarning("Demo seed skipped: set Demo:Password (a default password is only used in Development).");
            return;
        }

        if (await _users.FindByEmailAsync(demoEmail) is not null)
        {
            _log.LogInformation("Demo already seeded; skipping.");
            return;
        }

        var demo = await CreateUserAsync(demoEmail, demoPassword, "Dana Demo", "Demo Contracting LLC", isAdmin: false, PlanTier.Pro);

        // An admin account is only ever seeded in Development. In any other environment,
        // grant admin to your own account via the Admin:PromoteEmails setting instead.
        if (isDev && adminPassword is not null)
            await CreateUserAsync(adminEmail, adminPassword, "Avery Admin", "OppSignal", isAdmin: true, PlanTier.Pro);
        else
            _log.LogWarning("Admin demo account NOT seeded (Development-only). Use Admin:PromoteEmails to grant admin.");

        var now = _clock.UtcNow;
        _db.MatchProfiles.AddRange(
            new MatchProfile
            {
                UserId = demo.Id, Name = "IT & Software Services",
                Naics = new() { "541511", "541512", "541519", "518210" },
                NoticeTypes = new() { NoticeType.Solicitation, NoticeType.CombinedSynopsis, NoticeType.Presolicitation },
                IsActive = true, CreatedAt = now, UpdatedAt = now,
            },
            new MatchProfile
            {
                UserId = demo.Id, Name = "HVAC & Facilities",
                Naics = new() { "238220", "561720", "811310" },
                Keywords = new() { "HVAC", "chiller", "boiler" },
                IsActive = true, CreatedAt = now, UpdatedAt = now,
            },
            new MatchProfile
            {
                UserId = demo.Id, Name = "Small-Business Construction",
                Naics = new() { "236220", "237310", "238210" },
                SetAsides = new() { SetAsideCode.TotalSmallBusiness, SetAsideCode.EightA },
                IsActive = true, CreatedAt = now, UpdatedAt = now,
            });
        await _db.SaveChangesAsync(ct);

        // Bootstrap: pull a wide window so the demo dashboard is full and matches exist.
        var run = await _ingest.RunAsync(windowDaysOverride: 60, ct);
        _log.LogInformation("Demo seeded. Bootstrap ingest inserted {Ins} notices, {Matches} matches.",
            run.NoticesInserted, run.MatchesCreated);
    }

    private async Task<AppUser> CreateUserAsync(
        string email, string password, string fullName, string company, bool isAdmin, PlanTier plan)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // demo users skip verification
            FullName = fullName,
            CompanyName = company,
            TimeZoneId = "America/New_York",
            IsAdmin = isAdmin,
            CreatedAt = _clock.UtcNow,
        };
        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Demo user creation failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));

        _db.Subscriptions.Add(new Subscription
        {
            UserId = user.Id,
            Plan = plan,
            Status = SubscriptionStatus.Active,
            CurrentPeriodEndsAt = _clock.UtcNow.AddYears(1),
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        });
        await _db.SaveChangesAsync();
        return user;
    }
}
