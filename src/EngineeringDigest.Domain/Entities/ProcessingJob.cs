using EngineeringDigest.Domain.Enums;
using EngineeringDigest.Domain.SeedWork;

namespace EngineeringDigest.Domain.Entities;

public sealed class ProcessingJob : AuditableEntity
{
    public required string JobType { get; set; }
    public Guid? VideoId { get; set; }
    public Video? Video { get; set; }
    public Guid? ArticleId { get; set; }
    public Article? Article { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public ProcessingJobStatus Status { get; set; } = ProcessingJobStatus.Running;
    public string? FailureReason { get; set; }
    public string? CorrelationId { get; set; }
    public long? DurationMilliseconds => CompletedAt is null ? null : (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;

    public void Complete()
    {
        if (Status == ProcessingJobStatus.Completed)
        {
            return;
        }

        CompletedAt = DateTimeOffset.UtcNow;
        Status = ProcessingJobStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string reason)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        Status = ProcessingJobStatus.Failed;
        FailureReason = reason.Length > 4000 ? reason[..4000] : reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Abandon(string reason)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        Status = ProcessingJobStatus.Abandoned;
        FailureReason = reason.Length > 4000 ? reason[..4000] : reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
