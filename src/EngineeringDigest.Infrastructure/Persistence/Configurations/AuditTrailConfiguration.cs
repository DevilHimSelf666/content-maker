using EngineeringDigest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringDigest.Infrastructure.Persistence.Configurations;

public sealed class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Actor).HasMaxLength(256);
        builder.Property(x => x.Details).HasMaxLength(4000);
        builder.HasIndex(x => new { x.EntityName, x.EntityId, x.OccurredAt });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
