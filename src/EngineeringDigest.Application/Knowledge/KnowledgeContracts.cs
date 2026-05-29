namespace EngineeringDigest.Application.Knowledge;

public sealed record EmbeddingRequest(string Text, string Purpose);

public interface IEmbeddingProvider
{
    Task<float[]> GenerateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken);
}

public interface IKnowledgeService
{
    Task<Guid?> PromoteApprovedArticleAsync(Guid articleId, CancellationToken cancellationToken);
    Task<int> PromoteAllApprovedArticlesAsync(CancellationToken cancellationToken);
    Task<int> RebuildEmbeddingsAsync(int batchSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(KnowledgeSearchRequest request, CancellationToken cancellationToken);
    Task<int> RebuildRelatedArticlesAsync(CancellationToken cancellationToken);
    Task<int> AssignLearningPathsAsync(CancellationToken cancellationToken);
    Task<int> AnalyzeArticleQualityAsync(int batchSize, CancellationToken cancellationToken);
    Task<Guid> GenerateWeeklyDigestAsync(DateOnly weekStart, CancellationToken cancellationToken);
}

public interface IRagService
{
    Task<RagAnswer> AnswerAsync(string question, CancellationToken cancellationToken);
}

public sealed record KnowledgeSearchRequest(
    string? Query,
    bool UseSemanticSearch,
    Guid? CategoryId,
    IReadOnlyCollection<Guid> TagIds,
    int Take = 10);

public sealed record KnowledgeSearchResult(
    Guid Id,
    string Title,
    string? Summary,
    string? Category,
    decimal RelevanceScore,
    int ViewCount,
    IReadOnlyList<string> Tags);

public sealed record RagAnswer(string AnswerMarkdown, decimal ConfidenceScore, IReadOnlyList<RagSource> Sources);

public sealed record RagSource(Guid ArticleId, string Title, decimal RelevanceScore);
