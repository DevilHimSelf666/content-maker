using EngineeringDigest.Application.Articles;

namespace EngineeringDigest.Application.Abstractions;

public interface IArticleQualityScorer
{
    Task<ArticleQualityScore> ScoreAsync(string title, string contentMarkdown, CancellationToken cancellationToken);
}
