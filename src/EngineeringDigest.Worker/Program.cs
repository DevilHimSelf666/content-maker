using EngineeringDigest.Infrastructure.DependencyInjection;
using EngineeringDigest.Infrastructure.Messaging;
using EngineeringDigest.Infrastructure.Persistence;
using EngineeringDigest.Worker;
using Wolverine;

var app = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddEngineeringDigestInfrastructure(context.Configuration);
        services.AddHostedService<Worker>();
    })
    .UseWolverine(options =>
    {
        options.Discovery.IncludeAssembly(typeof(VideoWorkflowHandlers).Assembly);
    })
    .Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync(CancellationToken.None);
}

await app.RunAsync();
