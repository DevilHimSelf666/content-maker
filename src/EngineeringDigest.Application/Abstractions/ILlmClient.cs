using EngineeringDigest.Application.Articles;

namespace EngineeringDigest.Application.Abstractions;

public interface ILlmClient
{
    Task<VideoClassification> ClassifyAsync(string title, string? description, string transcript, CancellationToken cancellationToken);
    Task<GeneratedArticle> GenerateArticleAsync(string title, string? description, string transcript, CancellationToken cancellationToken);
    Task<string> AnswerFromContextAsync(string question, string contextMarkdown, CancellationToken cancellationToken);
    Task<string> GenerateDigestAsync(string digestInputsMarkdown, CancellationToken cancellationToken);
    Task<ArticleQualityEvaluation> EvaluateArticleQualityAsync(string title, string contentMarkdown, CancellationToken cancellationToken);
}
