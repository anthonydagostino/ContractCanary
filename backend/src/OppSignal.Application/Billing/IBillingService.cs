using OppSignal.Domain.Enums;

namespace OppSignal.Application.Billing;

public sealed record BillingConfig(bool Configured, string? PublishableKey);

/// <summary>
/// Billing port (Stripe). Kept behind an interface so the whole suite runs offline
/// with a fake; the real implementation talks to Stripe test mode via env config.
/// </summary>
public interface IBillingService
{
    BillingConfig GetConfig();

    /// <summary>Create a Checkout session for a plan and return the redirect URL.</summary>
    Task<string> CreateCheckoutSessionAsync(Guid userId, PlanTier plan, CancellationToken ct = default);

    /// <summary>Create a Billing Portal session and return the redirect URL.</summary>
    Task<string> CreatePortalSessionAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Verify + process a Stripe webhook, syncing the local subscription mirror.</summary>
    Task HandleWebhookAsync(string payload, string signatureHeader, CancellationToken ct = default);

    /// <summary>Best-effort cancel the user's Stripe subscription (no-op if billing is
    /// unconfigured or the user has none). Used on account deletion so a deleted user is never billed.</summary>
    Task TryCancelSubscriptionAsync(Guid userId, CancellationToken ct = default);
}
