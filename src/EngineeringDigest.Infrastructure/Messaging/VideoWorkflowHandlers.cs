using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Messaging;
using EngineeringDigest.Domain.Entities;
using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace EngineeringDigest.Infrastructure.Messaging;

public sealed class VideoWorkflowHandlers(
    EngineeringDigestDbContext dbContext,
    IYouTubeRssClient youTubeRssClient,
    ITranscriptClient transcriptClient,
    ILlmClient llmClient,
    ITelegramPublisher telegramPublisher,
    ILogger<VideoWorkflowHandlers> logger)
{
    public async Task Handle(DiscoverVideos command, IMessageBus bus, CancellationToken cancellationToken)
    {
        var channels = await dbContext.Channels.Where(x => x.IsEnabled).ToListAsync(cancellationToken);
        foreach (var channel in channels)
        {
            var videos = await youTubeRssClient.GetLatestVideosAsync(channel.RssFeedUrl, cancellationToken);
            foreach (var discovered in videos)
            {
                var exists = await dbContext.Videos.AnyAsync(x => x.YouTubeVideoId == discovered.YouTubeVideoId, cancellationToken);
                if (exists)
                {
                    logger.LogDebug("Ignoring duplicate video {YouTubeVideoId}.", discovered.YouTubeVideoId);
                    continue;
                }

                var video = new Video
                {
                    YouTubeVideoId = discovered.YouTubeVideoId,
                    Title = discovered.Title,
                    Description = discovered.Description,
                    Url = discovered.Url,
                    PublishedAt = discovered.PublishedAt,
                    ChannelId = channel.Id
                };
                dbContext.Videos.Add(video);
                await dbContext.SaveChangesAsync(cancellationToken);
                await bus.PublishAsync(new ExtractTranscript(video.Id));
            }

            channel.LastCheckedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ExtractTranscript command, IMessageBus bus, CancellationToken cancellationToken)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(x => x.Id == command.VideoId, cancellationToken);
        if (video is null || !string.IsNullOrWhiteSpace(video.Transcript))
        {
            return;
        }

        var transcript = await transcriptClient.GetTranscriptAsync(video.YouTubeVideoId, cancellationToken);
        video.MarkTranscriptReady(transcript);
        await dbContext.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new ClassifyVideo(video.Id));
    }

    public async Task Handle(ClassifyVideo command, IMessageBus bus, CancellationToken cancellationToken)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(x => x.Id == command.VideoId, cancellationToken);
        if (video is null || video.IsRelevant.HasValue || string.IsNullOrWhiteSpace(video.Transcript))
        {
            return;
        }

        var classification = await llmClient.ClassifyAsync(video.Title, video.Description, video.Transcript, cancellationToken);
        video.MarkClassified(classification.IsRelevant, classification.Score, classification.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (classification.IsRelevant)
        {
            await bus.PublishAsync(new GenerateArticle(video.Id));
        }
    }

    public async Task Handle(GenerateArticle command, CancellationToken cancellationToken)
    {
        var video = await dbContext.Videos.Include(x => x.Article).FirstOrDefaultAsync(x => x.Id == command.VideoId, cancellationToken);
        if (video is null || video.Article is not null || video.IsRelevant != true || string.IsNullOrWhiteSpace(video.Transcript))
        {
            return;
        }

        var generated = await llmClient.GenerateArticleAsync(video.Title, video.Description, video.Transcript, cancellationToken);
        video.Article = new Article
        {
            Title = generated.Title,
            ContentMarkdown = generated.ContentMarkdown,
            Summary = generated.Summary,
            Status = ArticleStatus.PendingReview
        };
        video.MarkPendingReview();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ApproveArticle command, CancellationToken cancellationToken)
    {
        var article = await dbContext.Articles.Include(x => x.Video).FirstOrDefaultAsync(x => x.Id == command.ArticleId, cancellationToken);
        if (article is null || article.Status is ArticleStatus.Approved or ArticleStatus.Published)
        {
            return;
        }

        article.Approve();
        article.Video?.MarkApproved();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(PublishArticle command, CancellationToken cancellationToken)
    {
        var article = await dbContext.Articles.Include(x => x.Video).FirstOrDefaultAsync(x => x.Id == command.ArticleId, cancellationToken);
        if (article is null || article.Status == ArticleStatus.Published)
        {
            return;
        }

        if (article.Status != ArticleStatus.Approved)
        {
            throw new InvalidOperationException("Articles must be manually approved before publishing to Telegram.");
        }

        var messageId = await telegramPublisher.PublishAsync(article.Title, article.ContentMarkdown, cancellationToken);
        article.MarkPublished(messageId);
        article.Video?.MarkPublished();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
