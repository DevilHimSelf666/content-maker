using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Articles;
using Microsoft.Extensions.Options;

namespace EngineeringDigest.Infrastructure.Llm;

public sealed class OpenAiCompatibleLlmClient(HttpClient httpClient, IOptions<LlmOptions> options) : ILlmClient
{
    private readonly LlmOptions _options = options.Value;

    public async Task<VideoClassification> ClassifyAsync(string prompt, CancellationToken cancellationToken)
    {
        var content = await CompleteAsync(prompt, cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        return new VideoClassification(
            root.GetProperty("isRelevant").GetBoolean(),
            root.GetProperty("score").GetDecimal(),
            root.GetProperty("reason").GetString() ?? string.Empty);
    }

    public async Task<GeneratedArticle> GenerateArticleAsync(string prompt, CancellationToken cancellationToken)
    {
        var content = await CompleteAsync(prompt, cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        return new GeneratedArticle(
            root.GetProperty("title").GetString() ?? string.Empty,
            root.GetProperty("contentMarkdown").GetString() ?? string.Empty,
            root.GetProperty("summary").GetString());
    }

    public async Task<ArticleQualityScore> ScoreArticleAsync(string prompt, CancellationToken cancellationToken)
    {
        var content = await CompleteAsync(prompt, cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        return new ArticleQualityScore(
            root.GetProperty("technicalDepth").GetDecimal(),
            root.GetProperty("relevance").GetDecimal(),
            root.GetProperty("readability").GetDecimal(),
            root.GetProperty("practicalValue").GetDecimal(),
            root.GetProperty("reason").GetString() ?? string.Empty);
    }

    private async Task<string> CompleteAsync(string userPrompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("Llm:Model must be configured. Models are never hardcoded.");
        }

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        var response = await httpClient.PostAsJsonAsync("/chat/completions", new
        {
            model = _options.Model,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = "You are a careful engineering editor. Output valid JSON only." },
                new { role = "user", content = userPrompt }
            }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";
    }
}
