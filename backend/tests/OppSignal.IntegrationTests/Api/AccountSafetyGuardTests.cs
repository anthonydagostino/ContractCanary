using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OppSignal.Infrastructure.Identity;
using OppSignal.Infrastructure.Persistence.Seed;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class AccountSafetyGuardTests : ApiTestBase
{
    public AccountSafetyGuardTests(OppSignalWebAppFactory factory) : base(factory) { }

    private static IConfiguration Config(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static async Task<(UserManager<AppUser> Users, AppUser User)> CreateUserAsync(
        IServiceScope scope, string email, string password)
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, CreatedAt = DateTime.UtcNow };
        (await users.CreateAsync(user, password)).Succeeded.Should().BeTrue();
        return (users, user);
    }

    [Fact]
    public async Task Promotes_accounts_listed_in_Admin_PromoteEmails()
    {
        var email = $"promote_{Guid.NewGuid():N}@test.dev";
        using var scope = Factory.Services.CreateScope();
        var (users, _) = await CreateUserAsync(scope, email, "Password123!");

        var guard = new AccountSafetyGuard(users,
            Config(new() { ["Admin:PromoteEmails"] = $"someone-else@x.dev, {email}" }),
            NullLogger<AccountSafetyGuard>.Instance);
        await guard.RunAsync(demoEnabled: true); // promotion runs regardless of demo mode

        (await users.FindByEmailAsync(email))!.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task Locks_a_default_account_that_still_uses_its_shipped_password_when_demo_is_off()
    {
        var email = $"legacy_{Guid.NewGuid():N}@test.dev";
        const string shipped = "ShippedDemoPass123!";
        using var scope = Factory.Services.CreateScope();
        var (users, _) = await CreateUserAsync(scope, email, shipped);

        var guard = new AccountSafetyGuard(users,
            Config(new() { ["Demo:Email"] = email, ["Demo:Password"] = shipped }),
            NullLogger<AccountSafetyGuard>.Instance);
        await guard.RunAsync(demoEnabled: false);

        (await users.IsLockedOutAsync((await users.FindByEmailAsync(email))!))
            .Should().BeTrue("a lingering default-credential account must be neutralized on a real deployment");
    }

    [Fact]
    public async Task Leaves_a_repurposed_account_alone_when_its_password_was_changed()
    {
        var email = $"repurposed_{Guid.NewGuid():N}@test.dev";
        using var scope = Factory.Services.CreateScope();
        var (users, _) = await CreateUserAsync(scope, email, "RealOwnerChosenPass123!");

        // The configured "default" password does NOT match this account's real password.
        var guard = new AccountSafetyGuard(users,
            Config(new() { ["Demo:Email"] = email, ["Demo:Password"] = "DemoPassword123!" }),
            NullLogger<AccountSafetyGuard>.Instance);
        await guard.RunAsync(demoEnabled: false);

        (await users.IsLockedOutAsync((await users.FindByEmailAsync(email))!))
            .Should().BeFalse("an account whose password was changed is owned by the operator and must not be locked");
    }

    [Fact]
    public async Task Does_not_lock_default_accounts_while_demo_mode_is_on()
    {
        var email = $"activedemo_{Guid.NewGuid():N}@test.dev";
        const string shipped = "ShippedDemoPass123!";
        using var scope = Factory.Services.CreateScope();
        var (users, _) = await CreateUserAsync(scope, email, shipped);

        var guard = new AccountSafetyGuard(users,
            Config(new() { ["Demo:Email"] = email, ["Demo:Password"] = shipped }),
            NullLogger<AccountSafetyGuard>.Instance);
        await guard.RunAsync(demoEnabled: true);

        (await users.IsLockedOutAsync((await users.FindByEmailAsync(email))!)).Should().BeFalse();
    }
}
