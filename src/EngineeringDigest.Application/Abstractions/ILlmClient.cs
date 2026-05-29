using EngineeringDigest.Application.Articles;

namespace EngineeringDigest.Application.Abstractions;

public interface ILlmClient
{
    Task<VideoClassification> ClassifyAsync(string title, string? description, string transcript, CancellationToken cancellationToken);
    Task<GeneratedArticle> GenerateArticleAsync(string title, string? description, string transcript, CancellationToken cancellationToken);
}
