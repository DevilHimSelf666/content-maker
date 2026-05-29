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


    public async Task<string> AnswerFromContextAsync(string question, string contextMarkdown, CancellationToken cancellationToken)
    {
        var prompt = $"""
        Answer the engineering question using only the supplied internal knowledge base context.
        Write in Persian and keep technical terms in English. If the context is insufficient, say so clearly.
        Cite source article titles inline.

        Question: {question}

        Context:
        {contextMarkdown[..Math.Min(contextMarkdown.Length, 14000)]}
        """;

        return await CompleteTextAsync(prompt, cancellationToken);
    }

    public async Task<string> GenerateDigestAsync(string digestInputsMarkdown, CancellationToken cancellationToken)
    {
        var prompt = $"""
        Generate a concise weekly Persian Engineering Digest for professional backend, .NET, architecture, DevOps, and AI engineers.
        Use only the provided inputs. Avoid hype and clickbait. Return markdown.

        Inputs:
        {digestInputsMarkdown[..Math.Min(digestInputsMarkdown.Length, 14000)]}
        """;

        return await CompleteTextAsync(prompt, cancellationToken);
    }

    public async Task<ArticleQualityEvaluation> EvaluateArticleQualityAsync(string title, string contentMarkdown, CancellationToken cancellationToken)
    {
        var prompt = $"""
        Evaluate this Persian engineering article. Return JSON only:
        {{"technicalDepth":0.0,"clarity":0.0,"relevance":0.0,"practicalValue":0.0,"notes":"short Persian notes"}}
        Scores must be between 0 and 1.

        Title: {title}
        Content: {contentMarkdown[..Math.Min(contentMarkdown.Length, 10000)]}
        """;

        var content = await CompleteAsync(prompt, cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        return new ArticleQualityEvaluation(
            root.GetProperty("technicalDepth").GetDecimal(),
            root.GetProperty("clarity").GetDecimal(),
            root.GetProperty("relevance").GetDecimal(),
            root.GetProperty("practicalValue").GetDecimal(),
            root.GetProperty("notes").GetString() ?? string.Empty);
    }

    private async Task<string> CompleteTextAsync(string userPrompt, CancellationToken cancellationToken)
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
            messages = new[]
            {
                new { role = "system", content = "You are a careful engineering editor." },
                new { role = "user", content = userPrompt }
            }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
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
