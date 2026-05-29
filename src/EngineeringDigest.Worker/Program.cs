using EngineeringDigest.Infrastructure.DependencyInjection;
using EngineeringDigest.Infrastructure.Messaging;
using EngineeringDigest.Infrastructure.Observability;
using EngineeringDigest.Infrastructure.Persistence;
using EngineeringDigest.Worker;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Wolverine;

var app = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console())
    .ConfigureServices((context, services) =>
    {
        services.AddEngineeringDigestInfrastructure(context.Configuration);
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics.AddHttpClientInstrumentation().AddMeter(EngineeringDigestMetrics.MeterName));
        services.AddHostedService<Worker>();
    })
    .UseWolverine((context, options) => options.ConfigureEngineeringDigestWolverine(context.Configuration))
    .Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(CancellationToken.None);
}

await app.RunAsync();
