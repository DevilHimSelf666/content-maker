using EngineeringDigest.Domain.SeedWork;

namespace EngineeringDigest.Domain.Entities;

public sealed class ArticleVersion : AuditableEntity
{
    public Guid ArticleId { get; set; }
    public Article? Article { get; set; }
    public int VersionNumber { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? PromptVersion { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public string? Summary { get; set; }
    public decimal? QualityScore { get; set; }
}
