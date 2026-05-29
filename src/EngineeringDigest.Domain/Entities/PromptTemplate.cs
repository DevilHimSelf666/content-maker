using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Domain.SeedWork;

namespace EngineeringDigest.Domain.Entities;

public sealed class PromptTemplate : AuditableEntity
{
    public PromptTemplateKind Kind { get; set; }
    public int Version { get; set; }
    public required string Name { get; set; }
    public required string Template { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
