using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Application.Billing;

namespace OppSignal.Infrastructure.Billing;

public static class BillingDependencyInjection
{
    public static IServiceCollection AddOppSignalBilling(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<StripeOptions>(config.GetSection(StripeOptions.SectionName));
        services.AddScoped<IBillingService, StripeBillingService>();
        return services;
    }
}
