using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Identity;
using OppSignal.Infrastructure.Persistence;
using Xunit;

namespace OppSignal.IntegrationTests.Support;

/// <summary>Base for API integration tests: resets the DB per test + auth helpers.</summary>
[Collection("api")]
public abstract class ApiTestBase : IAsyncLifetime
{
    protected readonly OppSignalWebAppFactory Factory;

    protected ApiTestBase(OppSignalWebAppFactory factory) => Factory = factory;

    public Task InitializeAsync() => Factory.Db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected HttpClient NewClient() => Factory.CreateClient();

    protected static HttpClient WithToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Register a user, confirm their email directly, and return them logged in.</summary>
    protected async Task<(HttpClient Client, string Token, Guid UserId)> RegisterAndLoginAsync(
        string email, string password = "Password123!")
    {
        var client = NewClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password, fullName = "Test User", companyName = "Test Co", timeZoneId = "America/New_York", acceptedTerms = true });
        reg.EnsureSuccessStatusCode();

        var userId = await ConfirmEmailDirectAsync(email);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<TokenResponse>();
        return (WithToken(client, tokens!.AccessToken), tokens.AccessToken, userId);
    }

    protected async Task<Guid> ConfirmEmailDirectAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException("user not found");
        user.EmailConfirmed = true;
        await users.UpdateAsync(user);
        return user.Id;
    }

    /// <summary>Force a user's subscription to a plan/status (bypassing Stripe) for enforcement tests.</summary>
    protected async Task SetPlanAsync(Guid userId, PlanTier plan, SubscriptionStatus status = SubscriptionStatus.Active)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (sub is null)
        {
            sub = new Domain.Entities.Subscription { UserId = userId, CreatedAt = DateTime.UtcNow };
            db.Subscriptions.Add(sub);
        }
        sub.Plan = plan;
        sub.Status = status;
        sub.CurrentPeriodEndsAt = DateTime.UtcNow.AddYears(1);
        sub.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    protected sealed record TokenResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<OppSignalWebAppFactory> { }
