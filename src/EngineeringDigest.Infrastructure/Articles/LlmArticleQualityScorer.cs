using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Articles;
using EngineeringDigest.Domain.Enums;

namespace EngineeringDigest.Infrastructure.Articles;

public sealed class LlmArticleQualityScorer(
    IPromptTemplateProvider promptTemplateProvider,
    ILlmClient llmClient) : IArticleQualityScorer
{
    public async Task<ArticleQualityScore> ScoreAsync(string title, string contentMarkdown, CancellationToken cancellationToken)
    {
        var template = await promptTemplateProvider.GetActiveTemplateAsync(PromptTemplateKind.QualityScoring, cancellationToken);
        var prompt = promptTemplateProvider.Render(template, new Dictionary<string, string?>
        {
            ["title"] = title,
            ["content"] = contentMarkdown
        });

        return await llmClient.ScoreArticleAsync(prompt, cancellationToken);
    }
}
