using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OppSignal.Application.Billing;
using OppSignal.Application.Common;

namespace OppSignal.Api.Controllers;

/// <summary>Public metadata for the marketing/pricing pages (branding + plan catalog).</summary>
[ApiController]
[Route("api/meta")]
[AllowAnonymous]
public sealed class MetaController : ControllerBase
{
    private readonly BrandingOptions _branding;

    public MetaController(IOptions<BrandingOptions> branding) => _branding = branding.Value;

    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        product = new
        {
            _branding.ProductName,
            _branding.Tagline,
            _branding.SupportEmail,
            _branding.CompanyLegalName,
        },
        trialDays = PlanCatalog.TrialDays,
        plans = PlanCatalog.Items.Select(p => new
        {
            tier = p.Tier.ToString(),
            p.Name,
            p.MonthlyPriceUsd,
            p.Blurb,
            p.Features,
        }),
    });
}
