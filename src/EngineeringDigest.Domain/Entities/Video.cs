using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Domain.SeedWork;

namespace EngineeringDigest.Domain.Entities;

public sealed class Video : AuditableEntity
{
    public required string YouTubeVideoId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Url { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public Guid ChannelId { get; set; }
    public Channel? Channel { get; set; }
    public VideoWorkflowStatus WorkflowStatus { get; set; } = VideoWorkflowStatus.Discovered;
    public string? Transcript { get; set; }
    public bool? IsRelevant { get; set; }
    public string? ClassificationReason { get; set; }
    public decimal? RelevanceScore { get; set; }
    public string? FailureReason { get; set; }
    public Article? Article { get; set; }

    public void MarkTranscriptReady(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            throw new ArgumentException("Transcript cannot be empty.", nameof(transcript));
        }

        Transcript = transcript.Trim();
        WorkflowStatus = VideoWorkflowStatus.TranscriptReady;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkClassified(bool isRelevant, decimal score, string reason)
    {
        IsRelevant = isRelevant;
        RelevanceScore = Math.Clamp(score, 0, 1);
        ClassificationReason = reason.Trim();
        WorkflowStatus = isRelevant ? VideoWorkflowStatus.Classified : VideoWorkflowStatus.NotRelevant;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPendingReview() => WorkflowStatus = VideoWorkflowStatus.PendingReview;
    public void MarkApproved() => WorkflowStatus = VideoWorkflowStatus.Approved;
    public void MarkPublished() => WorkflowStatus = VideoWorkflowStatus.Published;
}
