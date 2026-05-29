using System.Text;
using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Knowledge;
using EngineeringDigest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringDigest.Infrastructure.Knowledge;

public sealed class RagService(IKnowledgeService knowledgeService, KnowledgeDbContext knowledgeDbContext, ILlmClient llmClient) : IRagService
{
    public async Task<RagAnswer> AnswerAsync(string question, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        var matches = await knowledgeService.SearchAsync(new KnowledgeSearchRequest(question, true, null, [], 5), cancellationToken);
        var ids = matches.Select(x => x.Id).ToArray();
        var articles = await knowledgeDbContext.KnowledgeArticles.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var context = new StringBuilder();
        foreach (var match in matches)
        {
            var article = articles.First(x => x.Id == match.Id);
            context.AppendLine($"## Source: {article.Title}");
            context.AppendLine(article.Summary ?? article.KeyTakeaways);
            context.AppendLine(article.BodyMarkdown[..Math.Min(article.BodyMarkdown.Length, 3000)]);
            context.AppendLine();
        }

        var answer = await llmClient.AnswerFromContextAsync(question, context.ToString(), cancellationToken);
        var confidence = matches.Count == 0 ? 0m : Math.Clamp(matches.Average(x => x.RelevanceScore), 0m, 1m);
        return new RagAnswer(answer, confidence, matches.Select(x => new RagSource(x.Id, x.Title, x.RelevanceScore)).ToArray());
    }
}
