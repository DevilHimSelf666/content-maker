using EngineeringDigest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringDigest.Infrastructure.Persistence.Configurations;

public sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.TelegramMessageId).HasMaxLength(128);
        builder.Property(x => x.QualityScore).HasPrecision(4, 2);
        builder.Property(x => x.TechnicalDepthScore).HasPrecision(4, 2);
        builder.Property(x => x.RelevanceScore).HasPrecision(4, 2);
        builder.Property(x => x.ReadabilityScore).HasPrecision(4, 2);
        builder.Property(x => x.PracticalValueScore).HasPrecision(4, 2);
        builder.Property(x => x.ApprovedBy).HasMaxLength(256);
        builder.Property(x => x.RejectedBy).HasMaxLength(256);
        builder.Property(x => x.PublishedBy).HasMaxLength(256);
        builder.HasIndex(x => x.VideoId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
