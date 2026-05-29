using EngineeringDigest.Application.Channels;
using EngineeringDigest.Domain.Entities;
using EngineeringDigest.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EngineeringDigest.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public List<SeedChannelRequest> Channels { get; set; } = [];
}

public sealed class DbInitializer(
    EngineeringDigestDbContext dbContext,
    KnowledgeDbContext knowledgeDbContext,
    IOptions<SeedOptions> seedOptions,
    ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await knowledgeDbContext.Database.MigrateAsync(cancellationToken);

        foreach (var channel in seedOptions.Value.Channels)
        {
            var exists = await dbContext.Channels.AnyAsync(x => x.YouTubeChannelId == channel.YouTubeChannelId, cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.Channels.Add(new Channel
            {
                Name = channel.Name,
                YouTubeChannelId = channel.YouTubeChannelId,
                RssFeedUrl = channel.RssFeedUrl
            });
        }

        await SeedPromptAsync(PromptTemplateKind.Classification, "Default classification", 1, """
        You evaluate software engineering videos for Persian enterprise developers.
        Return JSON only: {"isRelevant": true|false, "score": 0.0-1.0, "reason": "short English reason"}.
        High value topics: .NET, ASP.NET Core, Blazor, EF Core, SQL Server, Clean Architecture, DDD, CQRS, Wolverine, DevOps, observability, security, AI engineering, RAG, system design.
        Low value: drama, hype, sponsorship, product marketing, shallow opinions.

        Title: {{title}}
        Description: {{description}}
        Transcript: {{transcript}}
        """, cancellationToken);

        await SeedPromptAsync(PromptTemplateKind.ArticleGeneration, "Default Persian engineering article", 1, """
        Write a practical Persian technical article for professional .NET, ASP.NET Core, Blazor, backend, architecture, and DevOps engineers.
        Keep technical terms in English. Avoid hype, clickbait, repetition, and unsupported claims.
        The article must be educational and actionable, not a summary.
        Return JSON only: {"title":"Persian title","summary":"short Persian summary","contentMarkdown":"markdown article"}.
        Required sections: Title, Introduction, Problem Statement, Technical Explanation, Enterprise Usage, Common Mistakes, Team Recommendations, Conclusion.

        Video title: {{title}}
        Description: {{description}}
        Transcript: {{transcript}}
        """, cancellationToken);

        await SeedPromptAsync(PromptTemplateKind.ArticleRegeneration, "Default article regeneration", 1, """
        Regenerate the Persian engineering article using a more practical, implementation-focused style.
        Preserve technical accuracy, keep technical terms in English, and avoid hype.
        Return JSON only: {"title":"Persian title","summary":"short Persian summary","contentMarkdown":"markdown article"}.

        Video title: {{title}}
        Description: {{description}}
        Transcript: {{transcript}}
        """, cancellationToken);

        await SeedPromptAsync(PromptTemplateKind.QualityScoring, "Default quality scoring", 1, """
        Score this Persian technical article from 0 to 10 for each criterion.
        Return JSON only: {"technicalDepth":0-10,"relevance":0-10,"readability":0-10,"practicalValue":0-10,"reason":"short English reason"}.
        Prefer actionable enterprise .NET, ASP.NET Core, Blazor, backend, architecture, DevOps, security, and observability content.

        Title: {{title}}
        Content: {{content}}
        """, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Database initialized and seed data applied.");
    }

    private async Task SeedPromptAsync(PromptTemplateKind kind, string name, int version, string template, CancellationToken cancellationToken)
    {
        var exists = await dbContext.PromptTemplates.AnyAsync(x => x.Kind == kind && x.Version == version, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.PromptTemplates.Add(new PromptTemplate
        {
            Kind = kind,
            Name = name,
            Version = version,
            Template = template,
            IsActive = true
        });
    }
}
