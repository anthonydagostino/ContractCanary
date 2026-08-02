using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OppSignal.Infrastructure.Identity;

namespace OppSignal.Infrastructure.Persistence.Seed;

/// <summary>
/// Startup account-safety pass, run after seeding. Two jobs:
/// <list type="number">
/// <item>Promote any accounts listed in <c>Admin:PromoteEmails</c> to admin
/// (idempotent) — the supported way to grant yourself admin without a seeded account.</item>
/// <item>When the demo is NOT being seeded (i.e. a real deployment), neutralize any
/// lingering default demo/admin accounts that still use their shipped password by
/// locking them out and logging a CRITICAL warning. Accounts whose password was
/// changed are left untouched (someone deliberately repurposed them).</item>
/// </list>
/// </summary>
public sealed class AccountSafetyGuard
{
    private readonly UserManager<AppUser> _users;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountSafetyGuard> _log;

    public AccountSafetyGuard(UserManager<AppUser> users, IConfiguration config, ILogger<AccountSafetyGuard> log)
    {
        _users = users;
        _config = config;
        _log = log;
    }

    public async Task RunAsync(bool demoEnabled, CancellationToken ct = default)
    {
        await PromoteConfiguredAdminsAsync();
        if (!demoEnabled) await NeutralizeDefaultAccountsAsync();
    }

    private async Task PromoteConfiguredAdminsAsync()
    {
        var emails = (_config["Admin:PromoteEmails"] ?? "")
            .Split(new[] { ',', ';', ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var raw in emails)
        {
            var user = await _users.FindByEmailAsync(raw.ToLowerInvariant());
            if (user is null)
            {
                _log.LogWarning("Admin:PromoteEmails lists {Email} but no such account exists yet.", raw);
                continue;
            }
            if (user.IsAdmin) continue;
            user.IsAdmin = true;
            await _users.UpdateAsync(user);
            _log.LogWarning("Granted admin to {Email} via Admin:PromoteEmails.", raw);
        }
    }

    private async Task NeutralizeDefaultAccountsAsync()
    {
        foreach (var (email, password) in DefaultAccounts())
        {
            var user = await _users.FindByEmailAsync(email);
            if (user is null) continue;
            if (user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow.AddYears(50)) continue; // already permanently locked
            if (!await _users.CheckPasswordAsync(user, password)) continue; // password changed → operator owns it, leave alone

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _users.UpdateAsync(user);
            _log.LogCritical(
                "SECURITY: locked seeded default account {Email} — it still used the shipped demo password. " +
                "Delete it, and grant admin to your own account via the Admin__PromoteEmails environment variable.",
                email);
        }
    }

    private IEnumerable<(string Email, string Password)> DefaultAccounts()
    {
        yield return ((_config["Demo:Email"] ?? "demo@oppsignal.dev").ToLowerInvariant(),
                      _config["Demo:Password"] ?? "DemoPassword123!");
        yield return ((_config["Demo:AdminEmail"] ?? "admin@oppsignal.dev").ToLowerInvariant(),
                      _config["Demo:AdminPassword"] ?? "AdminPassword123!");
    }
}
