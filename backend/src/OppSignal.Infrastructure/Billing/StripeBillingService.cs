using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Billing;
using OppSignal.Application.Common;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;
using Stripe;
using Stripe.Checkout;
using PlanTier = OppSignal.Domain.Enums.PlanTier;

namespace OppSignal.Infrastructure.Billing;

/// <summary>
/// Stripe-backed billing: Checkout (subscription mode, 14-day trial, no card
/// required), Billing Portal, and webhook processing that mirrors subscription
/// state into the local <c>Subscription</c> table. All config via env.
/// </summary>
public sealed class StripeBillingService : IBillingService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly StripeOptions _options;
    private readonly ILogger<StripeBillingService> _log;
    private readonly StripeClient? _client;

    public StripeBillingService(
        AppDbContext db, IClock clock, IOptions<StripeOptions> options, ILogger<StripeBillingService> log)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
        _log = log;
        _client = _options.IsConfigured ? new StripeClient(_options.SecretKey) : null;
    }

    public BillingConfig GetConfig() => new(_options.IsConfigured, _options.PublishableKey);

    public async Task<string> CreateCheckoutSessionAsync(Guid userId, PlanTier plan, CancellationToken ct = default)
    {
        EnsureConfigured();
        var priceId = PriceFor(plan)
            ?? throw new BadRequestException($"No Stripe price configured for the {plan} plan.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("User not found.");
        var sub = await GetOrCreateSubscriptionRowAsync(userId, ct);
        var customerId = await EnsureCustomerAsync(sub, user.Email!, user.FullName, ct);

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            ClientReferenceId = userId.ToString(),
            LineItems = new List<SessionLineItemOptions> { new() { Price = priceId, Quantity = 1 } },
            PaymentMethodCollection = "if_required", // no card required for the trial
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                TrialPeriodDays = _options.TrialDays,
                Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString(), ["plan"] = plan.ToString() },
            },
            SuccessUrl = _options.SuccessUrl,
            CancelUrl = _options.CancelUrl,
            Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString(), ["plan"] = plan.ToString() },
        };

        var session = await new SessionService(_client).CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(Guid userId, CancellationToken ct = default)
    {
        EnsureConfigured();
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (sub?.StripeCustomerId is null)
            throw new BadRequestException("No billing account yet. Start a subscription first.");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = sub.StripeCustomerId,
            ReturnUrl = _options.PortalReturnUrl,
        };
        var session = await new Stripe.BillingPortal.SessionService(_client).CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    public async Task HandleWebhookAsync(string payload, string signatureHeader, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            throw new BadRequestException("Stripe webhook secret is not configured.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _log.LogWarning("Rejected Stripe webhook: {Message}", ex.Message);
            throw new BadRequestException("Invalid webhook signature.");
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                if (stripeEvent.Data.Object is Session session && !string.IsNullOrEmpty(session.SubscriptionId))
                {
                    var sub = await new SubscriptionService(_client).GetAsync(session.SubscriptionId, cancellationToken: ct);
                    await SyncAsync(sub, session.ClientReferenceId, ct);
                }
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                if (stripeEvent.Data.Object is Subscription s)
                    await SyncAsync(s, null, ct);
                break;

            default:
                _log.LogDebug("Ignoring Stripe event {Type}", stripeEvent.Type);
                break;
        }
    }

    // ---- helpers ------------------------------------------------------------

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new BadRequestException("Billing is not configured on this deployment yet.");
    }

    private string? PriceFor(PlanTier plan) => plan switch
    {
        PlanTier.Pro => _options.ProPriceId,
        PlanTier.Starter => _options.StarterPriceId,
        _ => null,
    };

    private async Task<Domain.Entities.Subscription> GetOrCreateSubscriptionRowAsync(Guid userId, CancellationToken ct)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (sub is null)
        {
            sub = new Domain.Entities.Subscription
            {
                UserId = userId, Plan = PlanTier.None, Status = SubscriptionStatus.None,
                CreatedAt = _clock.UtcNow, UpdatedAt = _clock.UtcNow,
            };
            _db.Subscriptions.Add(sub);
            await _db.SaveChangesAsync(ct);
        }
        return sub;
    }

    private async Task<string> EnsureCustomerAsync(Domain.Entities.Subscription sub, string email, string? name, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(sub.StripeCustomerId)) return sub.StripeCustomerId!;

        var customer = await new CustomerService(_client).CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            Metadata = new Dictionary<string, string> { ["userId"] = sub.UserId.ToString() },
        }, cancellationToken: ct);

        sub.StripeCustomerId = customer.Id;
        sub.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return customer.Id;
    }

    private async Task SyncAsync(Subscription stripeSub, string? clientReferenceId, CancellationToken ct)
    {
        var userIdMeta = stripeSub.Metadata?.GetValueOrDefault("userId") ?? clientReferenceId;
        var priceId = stripeSub.Items?.Data?.FirstOrDefault()?.Price?.Id;

        var local = await FindLocalAsync(userIdMeta, stripeSub.CustomerId, stripeSub.Id, ct);
        if (local is null)
        {
            _log.LogWarning("No local subscription for Stripe sub {Sub} / customer {Cust}", stripeSub.Id, stripeSub.CustomerId);
            return;
        }

        local.StripeCustomerId = stripeSub.CustomerId;
        local.StripeSubscriptionId = stripeSub.Id;
        local.StripePriceId = priceId;
        local.Status = StripeSubscriptionMapper.MapStatus(stripeSub.Status);

        var mappedPlan = StripeSubscriptionMapper.MapPlan(priceId, _options.StarterPriceId, _options.ProPriceId);
        if (mappedPlan != PlanTier.None) local.Plan = mappedPlan;

        local.TrialEndsAt = stripeSub.TrialEnd;
        local.CurrentPeriodEndsAt = stripeSub.CurrentPeriodEnd;
        local.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
        local.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Synced subscription for user {User}: {Plan}/{Status}", local.UserId, local.Plan, local.Status);
    }

    private async Task<Domain.Entities.Subscription?> FindLocalAsync(string? userIdMeta, string? customerId, string? subscriptionId, CancellationToken ct)
    {
        if (Guid.TryParse(userIdMeta, out var uid))
        {
            var byUser = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == uid, ct);
            if (byUser is not null) return byUser;
        }
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            var byCust = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeCustomerId == customerId, ct);
            if (byCust is not null) return byCust;
        }
        if (!string.IsNullOrWhiteSpace(subscriptionId))
            return await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId, ct);
        return null;
    }
}
