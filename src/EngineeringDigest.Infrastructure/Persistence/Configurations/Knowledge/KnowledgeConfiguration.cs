using EngineeringDigest.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringDigest.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.BodyMarkdown).HasColumnType("text").IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.KeyTakeaways).HasColumnType("text");
        builder.Property(x => x.TitleEmbedding).HasColumnType("vector(1536)");
        builder.Property(x => x.BodyEmbedding).HasColumnType("vector(1536)");
        builder.Property(x => x.KeyTakeawaysEmbedding).HasColumnType("vector(1536)");
        builder.Property(x => x.QualityTechnicalDepth).HasPrecision(5, 4);
        builder.Property(x => x.QualityClarity).HasPrecision(5, 4);
        builder.Property(x => x.QualityRelevance).HasPrecision(5, 4);
        builder.Property(x => x.QualityPracticalValue).HasPrecision(5, 4);
        builder.Property(x => x.QualityNotes).HasMaxLength(2000);
        builder.HasIndex(x => x.ArticleId).IsUnique();
        builder.HasIndex(x => x.CategoryId);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.Ignore(x => x.Article);
        builder.HasOne(x => x.Category).WithMany(x => x.Articles).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class KnowledgeCategoryConfiguration : IEntityTypeConfiguration<KnowledgeCategory>
{
    public void Configure(EntityTypeBuilder<KnowledgeCategory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class KnowledgeTagConfiguration : IEntityTypeConfiguration<KnowledgeTag>
{
    public void Configure(EntityTypeBuilder<KnowledgeTag> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class KnowledgeArticleTagConfiguration : IEntityTypeConfiguration<KnowledgeArticleTag>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticleTag> builder)
    {
        builder.HasKey(x => new { x.KnowledgeArticleId, x.KnowledgeTagId });
        builder.HasOne(x => x.KnowledgeArticle).WithMany(x => x.Tags).HasForeignKey(x => x.KnowledgeArticleId);
        builder.HasOne(x => x.KnowledgeTag).WithMany(x => x.Articles).HasForeignKey(x => x.KnowledgeTagId);
    }
}

public sealed class KnowledgeReferenceConfiguration : IEntityTypeConfiguration<KnowledgeReference>
{
    public void Configure(EntityTypeBuilder<KnowledgeReference> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.KnowledgeArticle).WithMany(x => x.References).HasForeignKey(x => x.KnowledgeArticleId);
    }
}

public sealed class KnowledgeArticleRelationConfiguration : IEntityTypeConfiguration<KnowledgeArticleRelation>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticleRelation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Score).HasPrecision(5, 4);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.SourceArticleId, x.RelatedArticleId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.SourceArticle).WithMany(x => x.RelatedArticles).HasForeignKey(x => x.SourceArticleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RelatedArticle).WithMany().HasForeignKey(x => x.RelatedArticleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LearningPathConfiguration : IEntityTypeConfiguration<LearningPath>
{
    public void Configure(EntityTypeBuilder<LearningPath> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class LearningPathArticleConfiguration : IEntityTypeConfiguration<LearningPathArticle>
{
    public void Configure(EntityTypeBuilder<LearningPathArticle> builder)
    {
        builder.HasKey(x => new { x.LearningPathId, x.KnowledgeArticleId });
        builder.HasOne(x => x.LearningPath).WithMany(x => x.Articles).HasForeignKey(x => x.LearningPathId);
        builder.HasOne(x => x.KnowledgeArticle).WithMany(x => x.LearningPaths).HasForeignKey(x => x.KnowledgeArticleId);
    }
}

public sealed class WeeklyDigestConfiguration : IEntityTypeConfiguration<WeeklyDigest>
{
    public void Configure(EntityTypeBuilder<WeeklyDigest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentMarkdown).HasColumnType("text").IsRequired();
        builder.Property(x => x.TrendingTopics).HasMaxLength(2000);
        builder.HasIndex(x => x.WeekStart).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class KnowledgeSearchLogConfiguration : IEntityTypeConfiguration<KnowledgeSearchLog>
{
    public void Configure(EntityTypeBuilder<KnowledgeSearchLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Query).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SearchType).HasMaxLength(32).IsRequired();
    }
}
