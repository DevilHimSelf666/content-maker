using EngineeringDigest.Domain.Enums;

namespace EngineeringDigest.Application.Prompts;

public sealed record PromptTemplateResult(PromptTemplateKind Kind, int Version, string Template);
