using EngineeringDigest.Application.Channels;
using EngineeringDigest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EngineeringDigest.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public List<SeedChannelRequest> Channels { get; set; } = [];
}

public sealed class DbInitializer(
    EngineeringDigestDbContext dbContext,
    KnowledgeDbContext knowledgeDbContext,
    IOptions<SeedOptions> seedOptions,
    ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await knowledgeDbContext.Database.MigrateAsync(cancellationToken);

        foreach (var channel in seedOptions.Value.Channels)
        {
            var exists = await dbContext.Channels.AnyAsync(x => x.YouTubeChannelId == channel.YouTubeChannelId, cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.Channels.Add(new Channel
            {
                Name = channel.Name,
                YouTubeChannelId = channel.YouTubeChannelId,
                RssFeedUrl = channel.RssFeedUrl
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Database initialized and channel seed data applied.");
    }
}
