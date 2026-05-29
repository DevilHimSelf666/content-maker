using EngineeringDigest.Domain.Entities;
using EngineeringDigest.Domain.Enums;
using Xunit;

namespace EngineeringDigest.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public void Video_mark_transcript_ready_is_idempotent_state_transition()
    {
        var video = NewVideo();

        video.MarkTranscriptReady(" transcript ");

        Assert.Equal("transcript", video.Transcript);
        Assert.Equal(VideoWorkflowStatus.TranscriptReady, video.WorkflowStatus);
    }

    [Fact]
    public void Relevant_video_moves_to_classified()
    {
        var video = NewVideo();

        video.MarkClassified(true, 2m, "deep .NET content");

        Assert.True(video.IsRelevant);
        Assert.Equal(1m, video.RelevanceScore);
        Assert.Equal(VideoWorkflowStatus.Classified, video.WorkflowStatus);
    }

    [Fact]
    public void Generated_article_starts_pending_review_not_published()
    {
        var article = new Article
        {
            VideoId = Guid.CreateVersion7(),
            Title = "عنوان فارسی",
            ContentMarkdown = "محتوای آموزشی درباره ASP.NET Core",
            Status = ArticleStatus.PendingReview
        };

        Assert.Equal(ArticleStatus.PendingReview, article.Status);
        Assert.Null(article.PublishedAt);
    }

    [Fact]
    public void Article_must_be_approved_before_publish()
    {
        var article = new Article
        {
            VideoId = Guid.CreateVersion7(),
            Title = "عنوان فارسی",
            ContentMarkdown = "محتوا",
            Status = ArticleStatus.PendingReview
        };

        Assert.Throws<InvalidOperationException>(() => article.MarkPublished("42"));
    }

    private static Video NewVideo() => new()
    {
        YouTubeVideoId = "abc123",
        Title = "Deep dive into EF Core",
        Url = "https://www.youtube.com/watch?v=abc123",
        PublishedAt = DateTimeOffset.UtcNow,
        ChannelId = Guid.CreateVersion7()
    };
}
