using OppSignal.Application.Billing;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string? FullName, string? CompanyName, string? TimeZoneId);
public sealed record LoginRequest(string Email, string Password);
public sealed record ConfirmEmailRequest(string UserId, string Token);
public sealed record ResendVerificationRequest(string Email);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record RefreshRequest(string RefreshToken);
public sealed record UpdateAccountRequest(string? FullName, string? CompanyName, string? TimeZoneId);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record DeleteAccountRequest(string Password);

/// <summary>A user's own data, for the data-export ("download my data") right.</summary>
public sealed record AccountExport(
    object Account,
    IReadOnlyList<object> MatchProfiles,
    IReadOnlyList<object> SavedOpportunities,
    IReadOnlyList<object> Alerts,
    DateTime ExportedAtUtc);

/// <summary>Issued token pair. Access token is a JWT; refresh token is opaque and rotated.</summary>
public sealed record AuthTokens(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

public sealed class MeDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string TimeZoneId { get; set; } = "America/New_York";
    public bool IsAdmin { get; set; }

    public PlanTier Plan { get; set; }
    public PlanLimits Limits { get; set; } = PlanPolicy.None;
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEndsAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
}
