using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Prompts;
using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringDigest.Infrastructure.Prompts;

public sealed class PromptTemplateProvider(EngineeringDigestDbContext dbContext) : IPromptTemplateProvider
{
    public async Task<PromptTemplateResult> GetActiveTemplateAsync(PromptTemplateKind kind, CancellationToken cancellationToken)
    {
        var template = await dbContext.PromptTemplates.AsNoTracking()
            .Where(x => x.Kind == kind && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"No active prompt template exists for {kind}.");

        return new PromptTemplateResult(template.Kind, template.Version, template.Template);
    }

    public string Render(PromptTemplateResult template, IReadOnlyDictionary<string, string?> values)
    {
        var rendered = template.Template;
        foreach (var (key, value) in values)
        {
            rendered = rendered.Replace("{{" + key + "}}", value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return rendered;
    }
}
