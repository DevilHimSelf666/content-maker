using EngineeringDigest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringDigest.Infrastructure.Persistence.Configurations;

public sealed class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.JobType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.FailureReason).HasMaxLength(4000);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Ignore(x => x.DurationMilliseconds);
        builder.HasIndex(x => new { x.JobType, x.VideoId, x.ArticleId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.Video).WithMany(x => x.ProcessingJobs).HasForeignKey(x => x.VideoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.NoAction);
    }
}
