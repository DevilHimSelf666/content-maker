using EngineeringDigest.Application.Articles;

namespace EngineeringDigest.Application.Abstractions;

public interface ILlmClient
{
    Task<VideoClassification> ClassifyAsync(string prompt, CancellationToken cancellationToken);
    Task<GeneratedArticle> GenerateArticleAsync(string prompt, CancellationToken cancellationToken);
    Task<ArticleQualityScore> ScoreArticleAsync(string prompt, CancellationToken cancellationToken);
}
