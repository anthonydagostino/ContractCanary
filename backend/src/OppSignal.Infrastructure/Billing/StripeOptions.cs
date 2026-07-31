namespace OppSignal.Infrastructure.Billing;

/// <summary>Stripe config — all via env (Stripe__*). Test-mode keys/prices until go-live.</summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string? SecretKey { get; set; }
    public string? PublishableKey { get; set; }
    public string? WebhookSecret { get; set; }

    public string? StarterPriceId { get; set; }
    public string? ProPriceId { get; set; }

    public int TrialDays { get; set; } = 14;

    /// <summary>Where Checkout / Portal send the user back (usually the web app).</summary>
    public string SuccessUrl { get; set; } = "http://localhost:5173/app/settings?checkout=success";
    public string CancelUrl { get; set; } = "http://localhost:5173/app/settings?checkout=cancel";
    public string PortalReturnUrl { get; set; } = "http://localhost:5173/app/settings";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
