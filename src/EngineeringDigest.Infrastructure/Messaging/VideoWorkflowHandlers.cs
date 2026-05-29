using System.Diagnostics;
using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Messaging;
using EngineeringDigest.Application.Knowledge;
using EngineeringDigest.Domain.Entities;
using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Infrastructure.Observability;
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
    IPromptTemplateProvider promptTemplateProvider,
    IArticleQualityScorer articleQualityScorer,
    EngineeringDigestMetrics metrics,
    ILogger<VideoWorkflowHandlers> logger)
{
    public async Task Handle(DiscoverVideos command, IMessageBus bus, CancellationToken cancellationToken)
    {
        var job = await StartJobAsync(nameof(DiscoverVideos), null, null, cancellationToken);
        try
        {
            var channels = await dbContext.Channels.Where(x => x.IsEnabled).ToListAsync(cancellationToken);
            foreach (var channel in channels)
            {
                logger.LogInformation("Discovering videos for channel {ChannelId} {ChannelName}.", channel.Id, channel.Name);
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
                    metrics.VideoDiscovered();
                    logger.LogInformation("Discovered video {VideoId} {YouTubeVideoId}.", video.Id, video.YouTubeVideoId);
                    await bus.PublishAsync(new ExtractTranscript(video.Id));
                }

                channel.LastCheckedAt = DateTimeOffset.UtcNow;
            }

            job.Complete();
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await FailJobAsync(job, ex, cancellationToken);
            throw;
        }
    }

    public async Task Handle(ExtractTranscript command, IMessageBus bus, CancellationToken cancellationToken)
    {
        var job = await StartJobAsync(nameof(ExtractTranscript), command.VideoId, null, cancellationToken);
        try
        {
            var video = await dbContext.Videos.FirstOrDefaultAsync(x => x.Id == command.VideoId, cancellationToken);
            if (video is null || !string.IsNullOrWhiteSpace(video.Transcript))
            {
                job.Complete();
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            logger.LogInformation("Extracting transcript for video {VideoId} {YouTubeVideoId}.", video.Id, video.YouTubeVideoId);
            var transcript = await transcriptClient.GetTranscriptAsync(video.YouTubeVideoId, cancellationToken);
            video.MarkTranscriptReady(transcript);
            job.Complete();
            await dbContext.SaveChangesAsync(cancellationToken);
            await bus.PublishAsync(new ClassifyVideo(video.Id));
        }
        catch (Exception ex)
        {
            await FailVideoJobAsync(job, command.VideoId, ex, cancellationToken);
            throw;
        }
    }

    public async Task Handle(ClassifyVideo command, IMessageBus bus, CancellationToken cancellationToken)
    {
        var job = await StartJobAsync(nameof(ClassifyVideo), command.VideoId, null, cancellationToken);
        try
        {
            var video = await dbContext.Videos.FirstOrDefaultAsync(x => x.Id == command.VideoId, cancellationToken);
            if (video is null || video.IsRelevant.HasValue || string.IsNullOrWhiteSpace(video.Transcript))
            {
                job.Complete();
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            logger.LogInformation("Classifying video {VideoId}.", video.Id);
            var template = await promptTemplateProvider.GetActiveTemplateAsync(PromptTemplateKind.Classification, cancellationToken);
            var prompt = RenderVideoPrompt(template, video, 8000);
            var classification = await llmClient.ClassifyAsync(prompt, cancellationToken);
            video.MarkClassified(classification.IsRelevant, classification.Score, classification.Reason);
            job.Complete();
            await dbContext.SaveChangesAsync(cancellationToken);

            if (classification.IsRelevant)
            {
                await bus.PublishAsync(new GenerateArticle(video.Id));
            }
        }
        catch (Exception ex)
        {
            await FailVideoJobAsync(job, command.VideoId, ex, cancellationToken);
            throw;
        }
    }

    public async Task Handle(GenerateArticle command, CancellationToken cancellationToken)
    {
        var job = await StartJobAsync(nameof(GenerateArticle), command.VideoId, null, cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var video = await dbContext.Videos.Include(x => x.Article).FirstOrDefaultAsync(x => x.Id == command.VideoId, cancellationToken);
            if (video is null || video.Article is not null || video.IsRelevant != true || string.IsNullOrWhiteSpace(video.Transcript))
            {
                job.Complete();
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            logger.LogInformation("Generating article for video {VideoId}.", video.Id);
            var template = await promptTemplateProvider.GetActiveTemplateAsync(PromptTemplateKind.ArticleGeneration, cancellationToken);
            var prompt = RenderVideoPrompt(template, video, 12000);
            var generated = await llmClient.GenerateArticleAsync(prompt, cancellationToken);
            var article = new Article
            {
                VideoId = video.Id,
                Title = string.IsNullOrWhiteSpace(generated.Title) ? video.Title : generated.Title,
                ContentMarkdown = generated.ContentMarkdown,
                Summary = generated.Summary,
                Status = ArticleStatus.PendingReview,
                PromptVersion = template.Version
            };
            var score = await articleQualityScorer.ScoreAsync(article.Title, article.ContentMarkdown, cancellationToken);
            article.ApplyQualityScore(score.TechnicalDepth, score.Relevance, score.Readability, score.PracticalValue);
            video.Article = article;
            video.MarkPendingReview();
            await dbContext.SaveChangesAsync(cancellationToken);
            article.Versions.Add(new ArticleVersion
            {
                ArticleId = article.Id,
                VersionNumber = 1,
                PromptVersion = template.Version,
                Title = article.Title,
                Content = article.ContentMarkdown,
                Summary = article.Summary,
                QualityScore = article.QualityScore
            });
            job.ArticleId = article.Id;
            job.Complete();
            await dbContext.SaveChangesAsync(cancellationToken);
            metrics.ArticleGenerated(stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            await FailVideoJobAsync(job, command.VideoId, ex, cancellationToken);
            throw;
        }
    }

    public async Task Handle(RegenerateArticle command, CancellationToken cancellationToken)
    {
        var job = await StartJobAsync(nameof(RegenerateArticle), null, command.ArticleId, cancellationToken);
        try
        {
            var article = await dbContext.Articles.Include(x => x.Video).Include(x => x.Versions)
                .FirstOrDefaultAsync(x => x.Id == command.ArticleId, cancellationToken);
            if (article?.Video is null || string.IsNullOrWhiteSpace(article.Video.Transcript))
            {
                job.Complete();
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var template = command.PromptVersion.HasValue
                ? await dbContext.PromptTemplates.AsNoTracking().Where(x => x.Kind == PromptTemplateKind.ArticleRegeneration && x.Version == command.PromptVersion.Value).Select(x => new { x.Kind, x.Version, x.Template }).FirstAsync(cancellationToken)
                : null;
            var promptTemplate = template is null
                ? await promptTemplateProvider.GetActiveTemplateAsync(PromptTemplateKind.ArticleRegeneration, cancellationToken)
                : new EngineeringDigest.Application.Prompts.PromptTemplateResult(template.Kind, template.Version, template.Template);
            var prompt = RenderVideoPrompt(promptTemplate, article.Video, 12000);
            var generated = await llmClient.GenerateArticleAsync(prompt, cancellationToken);
            article.Title = string.IsNullOrWhiteSpace(generated.Title) ? article.Title : generated.Title;
            article.ContentMarkdown = generated.ContentMarkdown;
            article.Summary = generated.Summary;
            article.Status = ArticleStatus.PendingReview;
            article.PromptVersion = promptTemplate.Version;
            var score = await articleQualityScorer.ScoreAsync(article.Title, article.ContentMarkdown, cancellationToken);
            article.ApplyQualityScore(score.TechnicalDepth, score.Relevance, score.Readability, score.PracticalValue);
            article.Versions.Add(new ArticleVersion
            {
                ArticleId = article.Id,
                VersionNumber = article.Versions.Count == 0 ? 1 : article.Versions.Max(x => x.VersionNumber) + 1,
                PromptVersion = promptTemplate.Version,
                Title = article.Title,
                Content = article.ContentMarkdown,
                Summary = article.Summary,
                QualityScore = article.QualityScore
            });
            job.Complete();
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await FailJobAsync(job, ex, cancellationToken);
            throw;
        }
    }

    public async Task Handle(ApproveArticle command, IMessageBus bus, CancellationToken cancellationToken)
    {
        var article = await dbContext.Articles.Include(x => x.Video).FirstOrDefaultAsync(x => x.Id == command.ArticleId, cancellationToken);
        if (article is null || article.Status is ArticleStatus.Approved or ArticleStatus.Published)
        {
            return;
        }

        article.Approve(command.Actor);
        article.Video?.MarkApproved();
        AddAudit("Article", article.Id, "Approved", command.Actor, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        metrics.ArticleApproved();
    }

    public async Task Handle(RejectArticle command, CancellationToken cancellationToken)
    {
        var article = await dbContext.Articles.Include(x => x.Video).FirstOrDefaultAsync(x => x.Id == command.ArticleId, cancellationToken);
        if (article is null || article.Status == ArticleStatus.Published)
        {
            return;
        }

        article.Reject(command.Actor);
        if (article.Video is not null)
        {
            article.Video.WorkflowStatus = VideoWorkflowStatus.Rejected;
        }
        AddAudit("Article", article.Id, "Rejected", command.Actor, null);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(PublishArticle command, CancellationToken cancellationToken)
    {
        var job = await StartJobAsync(nameof(PublishArticle), null, command.ArticleId, cancellationToken);
        try
        {
            var article = await dbContext.Articles.Include(x => x.Video).FirstOrDefaultAsync(x => x.Id == command.ArticleId, cancellationToken);
            if (article is null || article.Status == ArticleStatus.Published)
            {
                job.Complete();
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            if (article.Status != ArticleStatus.Approved)
            {
                throw new InvalidOperationException("Articles must be manually approved before publishing to Telegram.");
            }

            logger.LogInformation("Publishing article {ArticleId} to Telegram.", article.Id);
            var messageId = await telegramPublisher.PublishAsync(article.Title, article.ContentMarkdown, cancellationToken);
            article.MarkPublished(messageId, command.Actor);
            article.Video?.MarkPublished();
            AddAudit("Article", article.Id, "Published", command.Actor, $"TelegramMessageId={messageId}");
            job.Complete();
            await dbContext.SaveChangesAsync(cancellationToken);
            metrics.ArticlePublished();
        }
        catch (Exception ex)
        {
            await FailJobAsync(job, ex, cancellationToken);
            throw;
        }
    }

    public async Task Handle(CleanupAbandonedJobs command, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-6);
        var jobs = await dbContext.ProcessingJobs.Where(x => x.Status == ProcessingJobStatus.Running && x.StartedAt < cutoff).ToListAsync(cancellationToken);
        foreach (var job in jobs)
        {
            job.Abandon("Job exceeded six hour running threshold.");
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(CleanupFailedTransientRecords command, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        await dbContext.ProcessingJobs.Where(x => x.Status == ProcessingJobStatus.Failed && x.CompletedAt < cutoff)
            .ExecuteUpdateAsync(x => x.SetProperty(j => j.IsDeleted, true), cancellationToken);
    }

    public async Task Handle(RefreshChannelMetadata command, CancellationToken cancellationToken)
    {
        await dbContext.Channels.Where(x => x.IsEnabled).ExecuteUpdateAsync(x => x.SetProperty(c => c.LastCheckedAt, DateTimeOffset.UtcNow), cancellationToken);
    }

    private string RenderVideoPrompt(EngineeringDigest.Application.Prompts.PromptTemplateResult template, Video video, int transcriptLimit) =>
        promptTemplateProvider.Render(template, new Dictionary<string, string?>
        {
            ["title"] = video.Title,
            ["description"] = video.Description,
            ["transcript"] = video.Transcript is null ? null : video.Transcript[..Math.Min(video.Transcript.Length, transcriptLimit)]
        });

    private async Task<ProcessingJob> StartJobAsync(string jobType, Guid? videoId, Guid? articleId, CancellationToken cancellationToken)
    {
        var job = new ProcessingJob
        {
            JobType = jobType,
            VideoId = videoId,
            ArticleId = articleId,
            CorrelationId = Activity.Current?.TraceId.ToString()
        };
        dbContext.ProcessingJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    private async Task FailVideoJobAsync(ProcessingJob job, Guid videoId, Exception exception, CancellationToken cancellationToken)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(x => x.Id == videoId, cancellationToken);
        video?.MarkFailed(exception.Message);
        await FailJobAsync(job, exception, cancellationToken);
    }

    private async Task FailJobAsync(ProcessingJob job, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Processing job {JobId} {JobType} failed.", job.Id, job.JobType);
        job.Fail(exception.Message);
        metrics.JobFailed();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddAudit(string entityName, Guid entityId, string action, string? actor, string? details)
    {
        dbContext.AuditTrails.Add(new AuditTrail
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Actor = actor,
            Details = details
        });
    }
}
