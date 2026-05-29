using EngineeringDigest.Domain.Knowledge;
using EngineeringDigest.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace EngineeringDigest.Infrastructure.Persistence;

public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<KnowledgeCategory> KnowledgeCategories => Set<KnowledgeCategory>();
    public DbSet<KnowledgeTag> KnowledgeTags => Set<KnowledgeTag>();
    public DbSet<KnowledgeReference> KnowledgeReferences => Set<KnowledgeReference>();
    public DbSet<KnowledgeArticleRelation> KnowledgeArticleRelations => Set<KnowledgeArticleRelation>();
    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();
    public DbSet<WeeklyDigest> WeeklyDigests => Set<WeeklyDigest>();
    public DbSet<KnowledgeSearchLog> KnowledgeSearchLogs => Set<KnowledgeSearchLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowledgeDbContext).Assembly, type => type.Namespace?.Contains(".Knowledge", StringComparison.Ordinal) == true);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditableEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditableEntities()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
