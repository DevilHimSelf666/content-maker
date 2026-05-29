using EngineeringDigest.Application.Prompts;
using EngineeringDigest.Domain.Enums;

namespace EngineeringDigest.Application.Abstractions;

public interface IPromptTemplateProvider
{
    Task<PromptTemplateResult> GetActiveTemplateAsync(PromptTemplateKind kind, CancellationToken cancellationToken);
    string Render(PromptTemplateResult template, IReadOnlyDictionary<string, string?> values);
}
