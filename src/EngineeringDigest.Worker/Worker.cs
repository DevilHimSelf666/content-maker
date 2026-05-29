using EngineeringDigest.Application.Messaging;
using Wolverine;

namespace EngineeringDigest.Worker;

public sealed class Worker(IMessageBus bus, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Engineering Digest worker started.");
        var lastCleanup = DateTimeOffset.MinValue;
        var lastMetadataRefresh = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.PublishAsync(new DiscoverVideos());

            var now = DateTimeOffset.UtcNow;
            if (now - lastCleanup > TimeSpan.FromHours(1))
            {
                await bus.PublishAsync(new CleanupAbandonedJobs());
                await bus.PublishAsync(new CleanupFailedTransientRecords());
                lastCleanup = now;
            }

            if (now - lastMetadataRefresh > TimeSpan.FromHours(12))
            {
                await bus.PublishAsync(new RefreshChannelMetadata());
                lastMetadataRefresh = now;
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
