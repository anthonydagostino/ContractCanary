using OppSignal.Domain.Enums;

namespace OppSignal.Application.Billing;

/// <summary>Display catalog for the pricing page (amounts are display-only; Stripe price ids come from config).</summary>
public sealed record PlanCatalogItem(
    PlanTier Tier,
    string Name,
    int MonthlyPriceUsd,
    string Blurb,
    IReadOnlyList<string> Features);

public static class PlanCatalog
{
    public const int TrialDays = PlanPolicy.TrialDays;

    public static readonly IReadOnlyList<PlanCatalogItem> Items = new[]
    {
        new PlanCatalogItem(
            PlanTier.Starter, "Starter", 29,
            "For a solo shop that needs to never miss a relevant opportunity.",
            new[]
            {
                "1 match profile",
                "Daily digest email",
                "Full opportunity search & detail",
                "Saved opportunities & deadline tracker",
            }),
        new PlanCatalogItem(
            PlanTier.Pro, "Pro", 79,
            "For teams tracking multiple lines of business.",
            new[]
            {
                "5 match profiles",
                "Daily digest email",
                "Priority ingest of your saved searches",
                "CSV export of search results",
                "Everything in Starter",
            }),
    };
}
