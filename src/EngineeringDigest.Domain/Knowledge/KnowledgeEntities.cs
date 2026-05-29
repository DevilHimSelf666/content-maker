using EngineeringDigest.Domain.Entities;
using EngineeringDigest.Domain.SeedWork;

namespace EngineeringDigest.Domain.Knowledge;

public sealed class KnowledgeArticle : AuditableEntity
{
    public Guid ArticleId { get; set; }
    public Article? Article { get; set; }
    public Guid? CategoryId { get; set; }
    public KnowledgeCategory? Category { get; set; }
    public required string Title { get; set; }
    public required string BodyMarkdown { get; set; }
    public string? Summary { get; set; }
    public string KeyTakeaways { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public int ViewCount { get; set; }
    public int UsefulCount { get; set; }
    public decimal? QualityTechnicalDepth { get; set; }
    public decimal? QualityClarity { get; set; }
    public decimal? QualityRelevance { get; set; }
    public decimal? QualityPracticalValue { get; set; }
    public string? QualityNotes { get; set; }
    public DateTimeOffset? QualityEvaluatedAt { get; set; }
    public DateTimeOffset? EmbeddingsUpdatedAt { get; set; }
    public string? TitleEmbedding { get; set; }
    public string? BodyEmbedding { get; set; }
    public string? KeyTakeawaysEmbedding { get; set; }
    public ICollection<KnowledgeArticleTag> Tags { get; set; } = new List<KnowledgeArticleTag>();
    public ICollection<KnowledgeReference> References { get; set; } = new List<KnowledgeReference>();
    public ICollection<KnowledgeArticleRelation> RelatedArticles { get; set; } = new List<KnowledgeArticleRelation>();
    public ICollection<LearningPathArticle> LearningPaths { get; set; } = new List<LearningPathArticle>();

    public void RefreshFromApprovedArticle(Article article)
    {
        if (string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.ContentMarkdown))
        {
            throw new InvalidOperationException("Approved article must have title and content before it can enter the knowledge base.");
        }

        Title = article.Title.Trim();
        BodyMarkdown = article.ContentMarkdown.Trim();
        Summary = string.IsNullOrWhiteSpace(article.Summary) ? null : article.Summary.Trim();
        KeyTakeaways = ExtractKeyTakeaways(BodyMarkdown, Summary);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordQuality(decimal technicalDepth, decimal clarity, decimal relevance, decimal practicalValue, string notes)
    {
        QualityTechnicalDepth = Clamp01(technicalDepth);
        QualityClarity = Clamp01(clarity);
        QualityRelevance = Clamp01(relevance);
        QualityPracticalValue = Clamp01(practicalValue);
        QualityNotes = notes.Trim();
        QualityEvaluatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void StoreEmbeddings(float[] titleEmbedding, float[] bodyEmbedding, float[] keyTakeawaysEmbedding)
    {
        TitleEmbedding = ToVectorLiteral(titleEmbedding);
        BodyEmbedding = ToVectorLiteral(bodyEmbedding);
        KeyTakeawaysEmbedding = ToVectorLiteral(keyTakeawaysEmbedding);
        EmbeddingsUpdatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static string ToVectorLiteral(IReadOnlyList<float> values) => "[" + string.Join(',', values.Select(x => x.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))) + "]";

    public static float[] FromVectorLiteral(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static string ExtractKeyTakeaways(string markdown, string? summary)
    {
        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.StartsWith("- ", StringComparison.Ordinal) || x.StartsWith("* ", StringComparison.Ordinal) || x.StartsWith("##", StringComparison.Ordinal))
            .Take(8)
            .Select(x => x.TrimStart('#', '-', '*', ' '));
        var takeaways = string.Join("\n", lines);
        return string.IsNullOrWhiteSpace(takeaways) ? summary ?? markdown[..Math.Min(markdown.Length, 1000)] : takeaways;
    }

    private static decimal Clamp01(decimal value) => Math.Clamp(value, 0m, 1m);
}

public sealed class KnowledgeCategory : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<KnowledgeArticle> Articles { get; set; } = new List<KnowledgeArticle>();
}

public sealed class KnowledgeTag : AuditableEntity
{
    public required string Name { get; set; }
    public ICollection<KnowledgeArticleTag> Articles { get; set; } = new List<KnowledgeArticleTag>();
}

public sealed class KnowledgeArticleTag
{
    public Guid KnowledgeArticleId { get; set; }
    public KnowledgeArticle? KnowledgeArticle { get; set; }
    public Guid KnowledgeTagId { get; set; }
    public KnowledgeTag? KnowledgeTag { get; set; }
}

public sealed class KnowledgeReference : AuditableEntity
{
    public Guid KnowledgeArticleId { get; set; }
    public KnowledgeArticle? KnowledgeArticle { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
}

public sealed class KnowledgeArticleRelation : AuditableEntity
{
    public Guid SourceArticleId { get; set; }
    public KnowledgeArticle? SourceArticle { get; set; }
    public Guid RelatedArticleId { get; set; }
    public KnowledgeArticle? RelatedArticle { get; set; }
    public decimal Score { get; set; }
    public required string Reason { get; set; }
}

public sealed class LearningPath : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<LearningPathArticle> Articles { get; set; } = new List<LearningPathArticle>();
}

public sealed class LearningPathArticle
{
    public Guid LearningPathId { get; set; }
    public LearningPath? LearningPath { get; set; }
    public Guid KnowledgeArticleId { get; set; }
    public KnowledgeArticle? KnowledgeArticle { get; set; }
    public int Order { get; set; }
}

public sealed class WeeklyDigest : AuditableEntity
{
    public DateOnly WeekStart { get; set; }
    public required string Title { get; set; }
    public required string ContentMarkdown { get; set; }
    public string? TrendingTopics { get; set; }
}

public sealed class KnowledgeSearchLog : AuditableEntity
{
    public required string Query { get; set; }
    public string SearchType { get; set; } = "FullText";
    public int ResultCount { get; set; }
}
