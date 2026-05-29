using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Domain.SeedWork;

namespace EngineeringDigest.Domain.Entities;

public sealed class Article : AuditableEntity
{
    public Guid VideoId { get; set; }
    public Video? Video { get; set; }
    public required string Title { get; set; }
    public required string ContentMarkdown { get; set; }
    public string? Summary { get; set; }
    public ArticleStatus Status { get; set; } = ArticleStatus.PendingReview;
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? TelegramMessageId { get; set; }
    public decimal? QualityScore { get; set; }
    public decimal? TechnicalDepthScore { get; set; }
    public decimal? RelevanceScore { get; set; }
    public decimal? ReadabilityScore { get; set; }
    public decimal? PracticalValueScore { get; set; }
    public int? PromptVersion { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectedBy { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public string? PublishedBy { get; set; }
    public ICollection<ArticleVersion> Versions { get; set; } = new List<ArticleVersion>();

    public void ApplyQualityScore(decimal technicalDepth, decimal relevance, decimal readability, decimal practicalValue)
    {
        TechnicalDepthScore = ClampScore(technicalDepth);
        RelevanceScore = ClampScore(relevance);
        ReadabilityScore = ClampScore(readability);
        PracticalValueScore = ClampScore(practicalValue);
        QualityScore = Math.Round((TechnicalDepthScore.Value + RelevanceScore.Value + ReadabilityScore.Value + PracticalValueScore.Value) / 4, 2);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Approve(string? actor = null)
    {
        if (Status == ArticleStatus.Published)
        {
            return;
        }

        Status = ArticleStatus.Approved;
        ApprovedAt ??= DateTimeOffset.UtcNow;
        ApprovedBy ??= actor;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string? actor = null)
    {
        if (Status == ArticleStatus.Published)
        {
            throw new InvalidOperationException("Published articles cannot be rejected.");
        }

        Status = ArticleStatus.Rejected;
        RejectedAt ??= DateTimeOffset.UtcNow;
        RejectedBy ??= actor;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPublished(string messageId, string? actor = null)
    {
        if (Status != ArticleStatus.Approved && Status != ArticleStatus.Published)
        {
            throw new InvalidOperationException("Only approved articles can be published.");
        }

        Status = ArticleStatus.Published;
        TelegramMessageId = messageId;
        PublishedAt ??= DateTimeOffset.UtcNow;
        PublishedBy ??= actor;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static decimal ClampScore(decimal value) => Math.Clamp(value, 0m, 10m);
}
