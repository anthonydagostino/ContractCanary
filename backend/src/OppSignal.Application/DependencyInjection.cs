using Microsoft.Extensions.DependencyInjection;
using OppSignal.Application.Ai;
using OppSignal.Application.Alerts;
using OppSignal.Application.Billing;
using OppSignal.Application.Ingest;
using OppSignal.Application.Matching;
using OppSignal.Application.Notices;
using OppSignal.Application.Profiles;
using OppSignal.Application.Reference;
using OppSignal.Application.Saved;

namespace OppSignal.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOppSignalApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMatchEngine, MatchEngine>();
        services.AddScoped<IMatchingService, MatchingService>();
        services.AddScoped<IIngestService, IngestService>();
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<INoticeService, NoticeService>();
        services.AddScoped<ISavedNoticeService, SavedNoticeService>();
        services.AddScoped<IReferenceService, ReferenceService>();
        services.AddScoped<ISummaryEnrichmentService, SummaryEnrichmentService>();
        services.AddScoped<IAlertService, AlertService>();
        return services;
    }
}
