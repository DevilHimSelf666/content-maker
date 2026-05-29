using EngineeringDigest.Application.Messaging;
using Wolverine;

namespace EngineeringDigest.Worker;

public sealed class Worker(IMessageBus bus, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Engineering Digest worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.PublishAsync(new DiscoverVideos());
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
