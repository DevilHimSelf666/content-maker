using EngineeringDigest.Domain.Entities;
using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Domain.Knowledge;
using Xunit;

namespace EngineeringDigest.Tests;

public sealed class KnowledgeTests
{
    [Fact]
    public void Approved_article_can_refresh_knowledge_article()
    {
        var article = new Article
        {
            Id = Guid.CreateVersion7(),
            VideoId = Guid.CreateVersion7(),
            Title = "آموزش EF Core Performance",
            ContentMarkdown = "## نکات کلیدی\n- از Index استفاده کنید\n- Query را اندازه‌گیری کنید",
            Summary = "راهنمای عملی Performance در EF Core",
            Status = ArticleStatus.Approved
        };
        var knowledge = new KnowledgeArticle
        {
            ArticleId = article.Id,
            Title = article.Title,
            BodyMarkdown = article.ContentMarkdown
        };

        knowledge.RefreshFromApprovedArticle(article);

        Assert.Equal(article.Title, knowledge.Title);
        Assert.Contains("Index", knowledge.KeyTakeaways, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Embeddings_are_stored_as_pgvector_literals()
    {
        var knowledge = new KnowledgeArticle
        {
            ArticleId = Guid.CreateVersion7(),
            Title = "ASP.NET Core",
            BodyMarkdown = "Content"
        };

        knowledge.StoreEmbeddings([1f, 0f], [0.5f, 0.5f], [0f, 1f]);

        Assert.Equal("[1,0]", knowledge.TitleEmbedding);
        Assert.Equal(new[] { 0.5f, 0.5f }, KnowledgeArticle.FromVectorLiteral(knowledge.BodyEmbedding));
        Assert.NotNull(knowledge.EmbeddingsUpdatedAt);
    }

    [Fact]
    public void Quality_scores_are_clamped()
    {
        var knowledge = new KnowledgeArticle
        {
            ArticleId = Guid.CreateVersion7(),
            Title = "Security",
            BodyMarkdown = "Content"
        };

        knowledge.RecordQuality(2m, -1m, 0.75m, 0.5m, "ok");

        Assert.Equal(1m, knowledge.QualityTechnicalDepth);
        Assert.Equal(0m, knowledge.QualityClarity);
        Assert.Equal(0.75m, knowledge.QualityRelevance);
    }
}
