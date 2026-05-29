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

    public void Approve()
    {
        if (Status == ArticleStatus.Published)
        {
            return;
        }

        Status = ArticleStatus.Approved;
        ApprovedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPublished(string messageId)
    {
        if (Status != ArticleStatus.Approved && Status != ArticleStatus.Published)
        {
            throw new InvalidOperationException("Only approved articles can be published.");
        }

        Status = ArticleStatus.Published;
        TelegramMessageId = messageId;
        PublishedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
