using OppSignal.Application;
using OppSignal.Application.Ai;
using OppSignal.Application.Awards;
using OppSignal.Application.Ingest;
using OppSignal.Infrastructure;
using OppSignal.Worker;
using OppSignal.Worker.Configuration;
using OppSignal.Worker.Jobs;
using Quartz;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, cfg) => cfg
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var config = builder.Configuration;

    builder.Services.AddOppSignalApplication();
    builder.Services.AddOppSignalInfrastructure(config);
    builder.Services.Configure<DigestOptions>(config.GetSection(DigestOptions.SectionName));

    var ingestOptions = config.GetSection(IngestOptions.SectionName).Get<IngestOptions>() ?? new IngestOptions();
    var intervalMinutes = Math.Max(5, ingestOptions.IntervalMinutes);

    var aiOptions = config.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
    var aiEnabled = aiOptions.Enabled && !string.IsNullOrWhiteSpace(aiOptions.ApiKey);
    var aiIntervalMinutes = Math.Max(2, aiOptions.IntervalMinutes);

    var awardsOptions = config.GetSection(AwardsOptions.SectionName).Get<AwardsOptions>() ?? new AwardsOptions();
    var awardsIntervalMinutes = Math.Max(60, awardsOptions.IntervalMinutes);

    // Wait for the DB (API owns migrations/seed) before Quartz fires anything.
    builder.Services.AddHostedService<DatabaseReadyGate>();

    builder.Services.AddQuartz(q =>
    {
        var ingestKey = new JobKey("ingest");
        q.AddJob<IngestJob>(o => o.WithIdentity(ingestKey));
        q.AddTrigger(t => t
            .ForJob(ingestKey)
            .WithIdentity("ingest-trigger")
            .StartAt(DateBuilder.FutureDate(30, IntervalUnit.Second)) // give the API time to migrate/seed
            // After downtime, skip missed runs and resume on schedule (no catch-up burst).
            .WithSimpleSchedule(s => s.WithIntervalInMinutes(intervalMinutes).RepeatForever()
                .WithMisfireHandlingInstructionNextWithRemainingCount()));

        var digestKey = new JobKey("digest");
        q.AddJob<DigestJob>(o => o.WithIdentity(digestKey));
        q.AddTrigger(t => t
            .ForJob(digestKey)
            .WithIdentity("digest-trigger")
            // A missed hour must NOT double-fire (belt-and-suspenders on top of the per-user local-hour filter).
            .WithCronSchedule("0 0 * * * ?", x => x.WithMisfireHandlingInstructionDoNothing()));

        // Recompete Radar: daily award pull from USAspending (or fixture).
        if (awardsOptions.Enabled)
        {
            var awardsKey = new JobKey("award-ingest");
            q.AddJob<AwardIngestJob>(o => o.WithIdentity(awardsKey));
            q.AddTrigger(t => t
                .ForJob(awardsKey)
                .WithIdentity("award-ingest-trigger")
                .StartAt(DateBuilder.FutureDate(90, IntervalUnit.Second)) // after first notice ingest settles
                .WithSimpleSchedule(s => s.WithIntervalInMinutes(awardsIntervalMinutes).RepeatForever()
                    .WithMisfireHandlingInstructionNextWithRemainingCount()));
        }

        // AI enrichment only runs when a key is configured; otherwise it's absent entirely.
        if (aiEnabled)
        {
            var aiKey = new JobKey("ai-summary");
            q.AddJob<SummaryEnrichmentJob>(o => o.WithIdentity(aiKey));
            q.AddTrigger(t => t
                .ForJob(aiKey)
                .WithIdentity("ai-summary-trigger")
                .StartAt(DateBuilder.FutureDate(45, IntervalUnit.Second))
                .WithSimpleSchedule(s => s.WithIntervalInMinutes(aiIntervalMinutes).RepeatForever()));
        }
    });

    builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

    var host = builder.Build();
    Log.Information("OppSignal Worker starting (ingest every {Interval}m, source {Source}, ai-summaries {Ai})",
        intervalMinutes, ingestOptions.Source, aiEnabled ? $"on ({aiOptions.Model}, every {aiIntervalMinutes}m)" : "off");
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OppSignal Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
