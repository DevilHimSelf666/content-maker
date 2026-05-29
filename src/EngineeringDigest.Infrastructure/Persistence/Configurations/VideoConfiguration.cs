using EngineeringDigest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringDigest.Infrastructure.Persistence.Configurations;

public sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.YouTubeVideoId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Transcript).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ClassificationReason).HasMaxLength(2000);
        builder.Property(x => x.RelevanceScore).HasPrecision(5, 4);
        builder.Property(x => x.WorkflowStatus).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.YouTubeVideoId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.Article).WithOne(x => x.Video).HasForeignKey<Article>(x => x.VideoId);
    }
}
