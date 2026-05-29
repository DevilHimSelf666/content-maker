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

    public async Task<VideoClassification> ClassifyAsync(string title, string? description, string transcript, CancellationToken cancellationToken)
    {
        var prompt = $"""
        You evaluate software engineering videos for Persian enterprise developers.
        Return JSON only: {{"isRelevant": true|false, "score": 0.0-1.0, "reason": "short English reason"}}.
        High value topics: .NET, ASP.NET Core, Blazor, EF Core, SQL Server, Clean Architecture, DDD, CQRS, Wolverine, DevOps, observability, security, AI engineering, RAG, system design.
        Low value: drama, hype, sponsorship, product marketing, shallow opinions.

        Title: {title}
        Description: {description}
        Transcript: {transcript[..Math.Min(transcript.Length, 8000)]}
        """;

        var content = await CompleteAsync(prompt, cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        return new VideoClassification(
            root.GetProperty("isRelevant").GetBoolean(),
            root.GetProperty("score").GetDecimal(),
            root.GetProperty("reason").GetString() ?? string.Empty);
    }

    public async Task<GeneratedArticle> GenerateArticleAsync(string title, string? description, string transcript, CancellationToken cancellationToken)
    {
        var prompt = $"""
        Write a practical Persian technical article for professional .NET, ASP.NET Core, Blazor, backend, architecture, and DevOps engineers.
        Keep technical terms in English. Avoid hype, clickbait, repetition, and unsupported claims.
        The article must be educational and actionable, not a summary.
        Return JSON only: {{"title":"Persian title","summary":"short Persian summary","contentMarkdown":"markdown article"}}.
        Required sections: Title, Introduction, Problem Statement, Technical Explanation, Enterprise Usage, Common Mistakes, Team Recommendations, Conclusion.

        Video title: {title}
        Description: {description}
        Transcript: {transcript[..Math.Min(transcript.Length, 12000)]}
        """;

        var content = await CompleteAsync(prompt, cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        return new GeneratedArticle(
            root.GetProperty("title").GetString() ?? title,
            root.GetProperty("contentMarkdown").GetString() ?? string.Empty,
            root.GetProperty("summary").GetString());
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
