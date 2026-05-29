using EngineeringDigest.Domain.SeedWork;

namespace EngineeringDigest.Domain.Entities;

public sealed class AuditTrail : AuditableEntity
{
    public required string EntityName { get; set; }
    public Guid EntityId { get; set; }
    public required string Action { get; set; }
    public string? Actor { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Details { get; set; }
}
