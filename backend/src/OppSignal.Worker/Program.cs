using OppSignal.Application;
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
            .WithSimpleSchedule(s => s.WithIntervalInMinutes(intervalMinutes).RepeatForever()));

        var digestKey = new JobKey("digest");
        q.AddJob<DigestJob>(o => o.WithIdentity(digestKey));
        q.AddTrigger(t => t
            .ForJob(digestKey)
            .WithIdentity("digest-trigger")
            .WithCronSchedule("0 0 * * * ?")); // top of every hour; the job filters by each user's local hour
    });

    builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

    var host = builder.Build();
    Log.Information("OppSignal Worker starting (ingest every {Interval}m, source {Source})",
        intervalMinutes, ingestOptions.Source);
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
