using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Auth;
using OppSignal.Application.Billing;
using OppSignal.Application.Common;
using OppSignal.Application.Email;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Auth;
using OppSignal.Infrastructure.Persistence;

namespace OppSignal.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity–backed auth: registration with email verification,
/// login issuing JWT access + rotating refresh tokens, password reset. New users
/// receive an auto-created 14-day Starter trial subscription (no card).
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _users;
    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokens;
    private readonly INotificationService _notifications;
    private readonly IEntitlementService _entitlements;
    private readonly IBillingService _billing;
    private readonly IClock _clock;
    private readonly AuthOptions _auth;
    private readonly BrandingOptions _branding;

    public AuthService(
        UserManager<AppUser> users,
        AppDbContext db,
        JwtTokenService tokens,
        INotificationService notifications,
        IEntitlementService entitlements,
        IBillingService billing,
        IClock clock,
        IOptions<AuthOptions> auth,
        IOptions<BrandingOptions> branding)
    {
        _users = users;
        _db = db;
        _tokens = tokens;
        _notifications = notifications;
        _entitlements = entitlements;
        _billing = billing;
        _clock = clock;
        _auth = auth.Value;
        _branding = branding.Value;
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _users.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Don't disclose that the account exists (enumeration). Email the real owner instead;
            // the endpoint returns the same generic "check your email" response either way.
            var signInUrl = $"{_branding.WebBaseUrl.TrimEnd('/')}/login";
            await _notifications.SendAccountExistsAsync(existing.Id, existing.Email!, existing.FullName, signInUrl, ct);
            return;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName?.Trim(),
            CompanyName = request.CompanyName?.Trim(),
            TimeZoneId = NormalizeTimeZone(request.TimeZoneId),
            CreatedAt = _clock.UtcNow,
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));

        // Auto-provision a 14-day Starter trial (no card).
        _db.Subscriptions.Add(new Subscription
        {
            UserId = user.Id,
            Plan = PlanTier.Starter,
            Status = SubscriptionStatus.Trialing,
            TrialEndsAt = _clock.UtcNow.AddDays(PlanPolicy.TrialDays),
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        await SendVerificationAsync(user, ct);
    }

    public async Task<AuthTokens> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null)
        {
            // Spend a comparable amount of time hashing so an unknown email can't be
            // distinguished from a wrong password by response timing (enumeration).
            _users.PasswordHasher.HashPassword(new AppUser(), request.Password);
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        if (await _users.IsLockedOutAsync(user))
            throw new ForbiddenAppException(
                "This account is temporarily locked after too many failed sign-in attempts. Please try again later.");

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user); // increments the lockout counter
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        await _users.ResetAccessFailedCountAsync(user);

        if (_auth.RequireConfirmedEmail && !await _users.IsEmailConfirmedAsync(user))
            throw new ForbiddenAppException("Please verify your email address before signing in.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = JwtTokenService.Hash(refreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null)
            throw new UnauthorizedAppException("Invalid or expired refresh token.");

        if (!existing.IsActive)
        {
            // A revoked/rotated token being presented again signals possible theft
            // (the legitimate holder already rotated it). Revoke the whole family so
            // both the attacker and the victim must re-authenticate.
            await RevokeAllAsync(existing.UserId, ct);
            throw new UnauthorizedAppException("Invalid or expired refresh token.");
        }

        var user = await _users.FindByIdAsync(existing.UserId.ToString())
                   ?? throw new UnauthorizedAppException("Invalid refresh token.");

        // Rotate.
        existing.RevokedAt = _clock.UtcNow;
        var tokens = await IssueTokensAsync(user, ct, replacing: existing);
        return tokens;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = JwtTokenService.Hash(refreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is { RevokedAt: null })
        {
            existing.RevokedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken ct = default)
    {
        if (!Guid.TryParse(request.UserId, out var id))
            throw new BadRequestException("Invalid confirmation link.");
        var user = await _users.FindByIdAsync(id.ToString())
                   ?? throw new BadRequestException("Invalid confirmation link.");
        if (await _users.IsEmailConfirmedAsync(user)) return; // idempotent

        var result = await _users.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            throw new BadRequestException("This confirmation link is invalid or has expired.");

        await _notifications.SendWelcomeAsync(user.Id, user.Email!, user.FullName, ct);
    }

    public async Task ResendVerificationAsync(string email, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(email.Trim().ToLowerInvariant());
        if (user is not null && !await _users.IsEmailConfirmedAsync(user))
            await SendVerificationAsync(user, ct);
        // Always succeed to avoid account enumeration.
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(email.Trim().ToLowerInvariant());
        if (user is not null)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var url = $"{_branding.WebBaseUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
            await _notifications.SendPasswordResetAsync(user.Id, user.Email!, user.FullName, url, ct);
        }
        // Always succeed to avoid account enumeration.
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant())
                   ?? throw new BadRequestException("This reset link is invalid or has expired.");
        var result = await _users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));

        // Invalidate outstanding refresh tokens on password reset.
        await RevokeAllAsync(user.Id, ct);
    }

    public async Task<MeDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");
        var entitlement = await _entitlements.GetAsync(userId, ct);

        return new MeDto
        {
            Id = user.Id,
            Email = user.Email!,
            EmailConfirmed = user.EmailConfirmed,
            FullName = user.FullName,
            CompanyName = user.CompanyName,
            TimeZoneId = user.TimeZoneId,
            IsAdmin = user.IsAdmin,
            Plan = entitlement.Plan,
            Limits = entitlement.Limits,
            SubscriptionStatus = entitlement.Subscription?.Status ?? SubscriptionStatus.None,
            TrialEndsAt = entitlement.Subscription?.TrialEndsAt,
            CurrentPeriodEndsAt = entitlement.Subscription?.CurrentPeriodEndsAt,
            CancelAtPeriodEnd = entitlement.Subscription?.CancelAtPeriodEnd ?? false,
        };
    }

    public async Task UpdateAccountAsync(Guid userId, UpdateAccountRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");
        if (request.FullName is not null) user.FullName = request.FullName.Trim();
        if (request.CompanyName is not null) user.CompanyName = request.CompanyName.Trim();
        if (request.TimeZoneId is not null) user.TimeZoneId = NormalizeTimeZone(request.TimeZoneId);

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    public async Task<AuthTokens> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");

        var result = await _users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            // Distinguish a wrong current password from a policy failure without leaking specifics.
            var passwordMismatch = result.Errors.Any(e => e.Code == "PasswordMismatch");
            throw new BadRequestException(passwordMismatch
                ? "Your current password is incorrect."
                : string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        // Changing the password logs out every other session, then re-issues this one.
        await RevokeAllAsync(user.Id, ct);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<AccountExport> ExportDataAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");

        var profiles = await _db.MatchProfiles.AsNoTracking().Where(p => p.UserId == userId)
            .Select(p => new { p.Name, p.Naics, p.Psc, p.Keywords, p.AgencyPaths, p.States, p.SetAsides, p.NoticeTypes, p.IsActive, p.CreatedAt })
            .ToListAsync(ct);
        var saved = await _db.SavedNotices.AsNoTracking().Where(s => s.UserId == userId)
            .Select(s => new { s.NoticeId, s.Note, s.Status, s.SavedAt })
            .ToListAsync(ct);
        var alerts = await _db.NoticeAlerts.AsNoTracking().Where(a => a.UserId == userId)
            .Select(a => new { a.NoticeId, a.Type, a.Message, a.CreatedAt })
            .ToListAsync(ct);

        var account = new { user.Email, user.FullName, user.CompanyName, user.TimeZoneId, user.CreatedAt };
        return new AccountExport(account, profiles, saved, alerts, _clock.UtcNow);
    }

    public async Task DeleteAccountAsync(Guid userId, string password, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");
        if (!await _users.CheckPasswordAsync(user, password))
            throw new BadRequestException("Your password is incorrect.");

        // Stop any recurring billing first so a deleted account is never charged again.
        await _billing.TryCancelSubscriptionAsync(userId, ct);

        // Purge all personal data (there is no FK cascade from the Identity user to the domain tables).
        await _db.NoticeAlerts.Where(a => a.UserId == userId).ExecuteDeleteAsync(ct);
        await _db.SavedNotices.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
        await _db.NoticeMatches.Where(m => m.UserId == userId).ExecuteDeleteAsync(ct);
        await _db.MatchProfiles.Where(p => p.UserId == userId).ExecuteDeleteAsync(ct);
        await _db.Subscriptions.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
        await _db.EmailLogs.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);
        await _db.RefreshTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(ct);

        await _users.DeleteAsync(user);
    }

    // ---- helpers ------------------------------------------------------------

    private async Task SendVerificationAsync(AppUser user, CancellationToken ct)
    {
        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var url = $"{_branding.WebBaseUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await _notifications.SendEmailVerificationAsync(user.Id, user.Email!, user.FullName, url, ct);
    }

    private async Task<AuthTokens> IssueTokensAsync(AppUser user, CancellationToken ct, RefreshToken? replacing = null)
    {
        var (access, accessExp) = _tokens.CreateAccessToken(user);
        var (refresh, refreshHash, refreshExp) = _tokens.CreateRefreshToken();

        var row = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = _clock.UtcNow,
            ExpiresAt = refreshExp,
        };
        if (replacing is not null) replacing.ReplacedByTokenHash = refreshHash;
        _db.RefreshTokens.Add(row);
        await _db.SaveChangesAsync(ct);

        return new AuthTokens(access, accessExp, refresh, refreshExp);
    }

    private async Task RevokeAllAsync(Guid userId, CancellationToken ct)
    {
        var active = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(ct);
        foreach (var t in active) t.RevokedAt = _clock.UtcNow;
        if (active.Count > 0) await _db.SaveChangesAsync(ct);
    }

    private static string NormalizeTimeZone(string? tz)
    {
        if (string.IsNullOrWhiteSpace(tz)) return "America/New_York";
        return TimeZoneInfo.TryFindSystemTimeZoneById(tz.Trim(), out _) ? tz.Trim() : "America/New_York";
    }
}
