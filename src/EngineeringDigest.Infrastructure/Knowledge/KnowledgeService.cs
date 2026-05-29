using System.Text;
using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Knowledge;
using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Domain.Knowledge;
using EngineeringDigest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringDigest.Infrastructure.Knowledge;

public sealed class KnowledgeService(
    EngineeringDigestDbContext contentDbContext,
    KnowledgeDbContext knowledgeDbContext,
    IEmbeddingProvider embeddingProvider,
    ILlmClient llmClient,
    ILogger<KnowledgeService> logger) : IKnowledgeService
{
    private static readonly string[] DefaultCategories = ["EF Core", "ASP.NET Core", "Architecture", "Security", "Performance", "AI", "DevOps"];
    private static readonly string[] DefaultLearningPaths = ["ASP.NET Core Path", "EF Core Path", "Architecture Path", "Security Path", "AI Engineering Path"];

    public async Task<Guid?> PromoteApprovedArticleAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await contentDbContext.Articles.Include(x => x.Video).AsNoTracking().FirstOrDefaultAsync(x => x.Id == articleId, cancellationToken);
        if (article is null || article.Status is not (ArticleStatus.Approved or ArticleStatus.Published))
        {
            return null;
        }

        await EnsureTaxonomyAsync(cancellationToken);
        var knowledgeArticle = await knowledgeDbContext.KnowledgeArticles.FirstOrDefaultAsync(x => x.ArticleId == article.Id, cancellationToken);
        if (knowledgeArticle is null)
        {
            knowledgeArticle = new KnowledgeArticle
            {
                ArticleId = article.Id,
                Title = article.Title,
                BodyMarkdown = article.ContentMarkdown,
                Summary = article.Summary
            };
            knowledgeDbContext.KnowledgeArticles.Add(knowledgeArticle);
        }

        knowledgeArticle.RefreshFromApprovedArticle(article);
        knowledgeArticle.CategoryId = await InferCategoryIdAsync(knowledgeArticle, cancellationToken);
        await knowledgeDbContext.SaveChangesAsync(cancellationToken);

        if (!await knowledgeDbContext.KnowledgeReferences.AnyAsync(x => x.KnowledgeArticleId == knowledgeArticle.Id, cancellationToken) && article.Video is not null)
        {
            knowledgeDbContext.KnowledgeReferences.Add(new KnowledgeReference
            {
                KnowledgeArticleId = knowledgeArticle.Id,
                Title = article.Video.Title,
                Url = article.Video.Url
            });
            await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        }

        return knowledgeArticle.Id;
    }

    public async Task<int> PromoteAllApprovedArticlesAsync(CancellationToken cancellationToken)
    {
        var ids = await contentDbContext.Articles.AsNoTracking()
            .Where(x => x.Status == ArticleStatus.Approved || x.Status == ArticleStatus.Published)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var promoted = 0;
        foreach (var id in ids)
        {
            if (await PromoteApprovedArticleAsync(id, cancellationToken) is not null)
            {
                promoted++;
            }
        }

        return promoted;
    }

    public async Task<int> RebuildEmbeddingsAsync(int batchSize, CancellationToken cancellationToken)
    {
        var articles = await knowledgeDbContext.KnowledgeArticles
            .Where(x => x.EmbeddingsUpdatedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(Math.Clamp(batchSize, 1, 100))
            .ToListAsync(cancellationToken);

        foreach (var article in articles)
        {
            var title = await embeddingProvider.GenerateEmbeddingAsync(new EmbeddingRequest(article.Title, "KnowledgeTitle"), cancellationToken);
            var body = await embeddingProvider.GenerateEmbeddingAsync(new EmbeddingRequest(article.BodyMarkdown, "KnowledgeBody"), cancellationToken);
            var takeaways = await embeddingProvider.GenerateEmbeddingAsync(new EmbeddingRequest(article.KeyTakeaways, "KnowledgeTakeaways"), cancellationToken);
            article.StoreEmbeddings(title, body, takeaways);
        }

        await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        return articles.Count;
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(KnowledgeSearchRequest request, CancellationToken cancellationToken)
    {
        var query = knowledgeDbContext.KnowledgeArticles.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Tags).ThenInclude(x => x.KnowledgeTag)
            .AsQueryable();

        if (request.CategoryId is not null)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId);
        }

        if (request.TagIds.Count > 0)
        {
            query = query.Where(x => x.Tags.Any(t => request.TagIds.Contains(t.KnowledgeTagId)));
        }

        var articles = await query.Take(200).ToListAsync(cancellationToken);
        float[]? questionEmbedding = null;
        if (request.UseSemanticSearch && !string.IsNullOrWhiteSpace(request.Query))
        {
            questionEmbedding = await embeddingProvider.GenerateEmbeddingAsync(new EmbeddingRequest(request.Query, "KnowledgeSearch"), cancellationToken);
        }

        var results = articles.Select(article => new KnowledgeSearchResult(
                article.Id,
                article.Title,
                article.Summary,
                article.Category?.Name,
                Score(article, request.Query, questionEmbedding),
                article.ViewCount,
                article.Tags.Select(x => x.KnowledgeTag?.Name).Where(x => x is not null).Select(x => x!).ToArray()))
            .Where(x => string.IsNullOrWhiteSpace(request.Query) || x.RelevanceScore > 0)
            .OrderByDescending(x => x.RelevanceScore)
            .ThenByDescending(x => x.ViewCount)
            .Take(Math.Clamp(request.Take, 1, 50))
            .ToArray();

        knowledgeDbContext.KnowledgeSearchLogs.Add(new KnowledgeSearchLog
        {
            Query = request.Query ?? string.Empty,
            SearchType = request.UseSemanticSearch ? "Semantic" : "FullText",
            ResultCount = results.Length
        });
        await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        return results;
    }

    public async Task<int> RebuildRelatedArticlesAsync(CancellationToken cancellationToken)
    {
        var articles = await knowledgeDbContext.KnowledgeArticles.Include(x => x.Tags).ThenInclude(x => x.KnowledgeTag).ToListAsync(cancellationToken);
        knowledgeDbContext.KnowledgeArticleRelations.RemoveRange(knowledgeDbContext.KnowledgeArticleRelations);
        var created = 0;
        foreach (var article in articles)
        {
            var related = articles.Where(x => x.Id != article.Id)
                .Select(x => new { Article = x, Score = RelatedScore(article, x) })
                .Where(x => x.Score >= 0.25m)
                .OrderByDescending(x => x.Score)
                .Take(5);
            foreach (var item in related)
            {
                knowledgeDbContext.KnowledgeArticleRelations.Add(new KnowledgeArticleRelation
                {
                    SourceArticleId = article.Id,
                    RelatedArticleId = item.Article.Id,
                    Score = item.Score,
                    Reason = "Similar category, technologies, tags, or semantic embedding."
                });
                created++;
            }
        }

        await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<int> AssignLearningPathsAsync(CancellationToken cancellationToken)
    {
        await EnsureTaxonomyAsync(cancellationToken);
        var paths = await knowledgeDbContext.LearningPaths.ToListAsync(cancellationToken);
        var articles = await knowledgeDbContext.KnowledgeArticles.Include(x => x.Category).ToListAsync(cancellationToken);
        var created = 0;
        foreach (var article in articles)
        {
            foreach (var path in paths.Where(path => BelongsToPath(article, path.Name)))
            {
                var exists = await knowledgeDbContext.Set<LearningPathArticle>().AnyAsync(x => x.KnowledgeArticleId == article.Id && x.LearningPathId == path.Id, cancellationToken);
                if (exists)
                {
                    continue;
                }

                knowledgeDbContext.Set<LearningPathArticle>().Add(new LearningPathArticle
                {
                    KnowledgeArticleId = article.Id,
                    LearningPathId = path.Id,
                    Order = await knowledgeDbContext.Set<LearningPathArticle>().CountAsync(x => x.LearningPathId == path.Id, cancellationToken) + 1
                });
                created++;
            }
        }

        await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<int> AnalyzeArticleQualityAsync(int batchSize, CancellationToken cancellationToken)
    {
        var articles = await knowledgeDbContext.KnowledgeArticles
            .Where(x => x.QualityEvaluatedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(Math.Clamp(batchSize, 1, 100))
            .ToListAsync(cancellationToken);

        foreach (var article in articles)
        {
            var quality = await llmClient.EvaluateArticleQualityAsync(article.Title, article.BodyMarkdown, cancellationToken);
            article.RecordQuality(quality.TechnicalDepth, quality.Clarity, quality.Relevance, quality.PracticalValue, quality.Notes);
        }

        await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        return articles.Count;
    }

    public async Task<Guid> GenerateWeeklyDigestAsync(DateOnly weekStart, CancellationToken cancellationToken)
    {
        var existing = await knowledgeDbContext.WeeklyDigests.FirstOrDefaultAsync(x => x.WeekStart == weekStart, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var articles = await knowledgeDbContext.KnowledgeArticles.Include(x => x.Category)
            .OrderByDescending(x => x.ViewCount + x.UsefulCount)
            .ThenByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
        var inputs = new StringBuilder();
        foreach (var article in articles)
        {
            inputs.AppendLine($"- {article.Title} ({article.Category?.Name ?? "Uncategorized"}): {article.Summary ?? article.KeyTakeaways}");
        }

        var content = await llmClient.GenerateDigestAsync(inputs.ToString(), cancellationToken);
        var digest = new WeeklyDigest
        {
            WeekStart = weekStart,
            Title = $"Engineering Digest - {weekStart:yyyy-MM-dd}",
            ContentMarkdown = content,
            TrendingTopics = string.Join(", ", articles.Select(x => x.Category?.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        };
        knowledgeDbContext.WeeklyDigests.Add(digest);
        await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        return digest.Id;
    }

    private async Task EnsureTaxonomyAsync(CancellationToken cancellationToken)
    {
        foreach (var category in DefaultCategories)
        {
            if (!await knowledgeDbContext.KnowledgeCategories.AnyAsync(x => x.Name == category, cancellationToken))
            {
                knowledgeDbContext.KnowledgeCategories.Add(new KnowledgeCategory { Name = category });
            }
        }

        foreach (var path in DefaultLearningPaths)
        {
            if (!await knowledgeDbContext.LearningPaths.AnyAsync(x => x.Name == path, cancellationToken))
            {
                knowledgeDbContext.LearningPaths.Add(new LearningPath { Name = path });
            }
        }

        await knowledgeDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid?> InferCategoryIdAsync(KnowledgeArticle article, CancellationToken cancellationToken)
    {
        var categories = await knowledgeDbContext.KnowledgeCategories.ToListAsync(cancellationToken);
        var text = article.Title + " " + article.BodyMarkdown;
        var match = categories.FirstOrDefault(x => text.Contains(x.Name, StringComparison.OrdinalIgnoreCase))
            ?? categories.FirstOrDefault(x => x.Name == "Architecture");
        return match?.Id;
    }

    private static decimal Score(KnowledgeArticle article, string? query, float[]? embedding)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return 1m;
        }

        var textScore = article.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ? 0.9m :
            article.BodyMarkdown.Contains(query, StringComparison.OrdinalIgnoreCase) ? 0.6m :
            article.KeyTakeaways.Contains(query, StringComparison.OrdinalIgnoreCase) ? 0.5m : 0m;
        if (embedding is null)
        {
            return textScore;
        }

        var semanticScore = new[] { article.TitleEmbedding, article.BodyEmbedding, article.KeyTakeawaysEmbedding }
            .Select(KnowledgeArticle.FromVectorLiteral)
            .Where(x => x.Length == embedding.Length)
            .Select(x => Cosine(x, embedding))
            .DefaultIfEmpty(0m)
            .Max();
        return Math.Max(textScore, semanticScore);
    }

    private static decimal RelatedScore(KnowledgeArticle left, KnowledgeArticle right)
    {
        var score = left.CategoryId == right.CategoryId ? 0.35m : 0m;
        var leftTags = left.Tags.Select(x => x.KnowledgeTag?.Name).Where(x => x is not null).ToHashSet(StringComparer.OrdinalIgnoreCase);
        score += right.Tags.Count(x => x.KnowledgeTag?.Name is not null && leftTags.Contains(x.KnowledgeTag.Name)) * 0.2m;
        var leftEmbedding = KnowledgeArticle.FromVectorLiteral(left.BodyEmbedding);
        var rightEmbedding = KnowledgeArticle.FromVectorLiteral(right.BodyEmbedding);
        if (leftEmbedding.Length == rightEmbedding.Length && leftEmbedding.Length > 0)
        {
            score = Math.Max(score, Cosine(leftEmbedding, rightEmbedding));
        }
        return Math.Min(score, 1m);
    }

    private static decimal Cosine(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var dot = 0d;
        var leftMagnitude = 0d;
        var rightMagnitude = 0d;
        for (var i = 0; i < left.Count; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0m;
        }

        return (decimal)(dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude)));
    }

    private static bool BelongsToPath(KnowledgeArticle article, string pathName)
    {
        var text = article.Title + " " + article.BodyMarkdown + " " + article.Category?.Name;
        return pathName switch
        {
            "ASP.NET Core Path" => text.Contains("ASP.NET", StringComparison.OrdinalIgnoreCase) || text.Contains("Blazor", StringComparison.OrdinalIgnoreCase),
            "EF Core Path" => text.Contains("EF Core", StringComparison.OrdinalIgnoreCase) || text.Contains("Entity Framework", StringComparison.OrdinalIgnoreCase),
            "Architecture Path" => text.Contains("Architecture", StringComparison.OrdinalIgnoreCase) || text.Contains("Clean Architecture", StringComparison.OrdinalIgnoreCase),
            "Security Path" => text.Contains("Security", StringComparison.OrdinalIgnoreCase) || text.Contains("Authentication", StringComparison.OrdinalIgnoreCase),
            "AI Engineering Path" => text.Contains("AI", StringComparison.OrdinalIgnoreCase) || text.Contains("RAG", StringComparison.OrdinalIgnoreCase) || text.Contains("LLM", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
