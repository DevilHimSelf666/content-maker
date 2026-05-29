using EngineeringDigest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringDigest.Infrastructure.Persistence.Configurations;

public sealed class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.YouTubeChannelId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RssFeedUrl).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => x.YouTubeChannelId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasMany(x => x.Videos).WithOne(x => x.Channel).HasForeignKey(x => x.ChannelId);
    }
}
