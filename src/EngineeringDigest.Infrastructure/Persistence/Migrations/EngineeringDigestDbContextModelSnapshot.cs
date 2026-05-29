using System;
using EngineeringDigest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace EngineeringDigest.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EngineeringDigestDbContext))]
public sealed class EngineeringDigestDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity("EngineeringDigest.Domain.Entities.Channel", b =>
        {
            b.Property<Guid>("Id");
            b.Property<DateTimeOffset>("CreatedAt");
            b.Property<DateTimeOffset?>("DeletedAt");
            b.Property<bool>("IsDeleted");
            b.Property<bool>("IsEnabled");
            b.Property<DateTimeOffset?>("LastCheckedAt");
            b.Property<string>("Name").IsRequired().HasMaxLength(200);
            b.Property<string>("RssFeedUrl").IsRequired().HasMaxLength(1000);
            b.Property<DateTimeOffset?>("UpdatedAt");
            b.Property<string>("YouTubeChannelId").IsRequired().HasMaxLength(128);
            b.HasKey("Id");
            b.HasIndex("YouTubeChannelId").IsUnique();
        });

        modelBuilder.Entity("EngineeringDigest.Domain.Entities.Video", b =>
        {
            b.Property<Guid>("Id");
            b.Property<Guid>("ChannelId");
            b.Property<string>("ClassificationReason").HasMaxLength(2000);
            b.Property<DateTimeOffset>("CreatedAt");
            b.Property<DateTimeOffset?>("DeletedAt");
            b.Property<string>("Description");
            b.Property<string>("FailureReason");
            b.Property<bool>("IsDeleted");
            b.Property<bool?>("IsRelevant");
            b.Property<DateTimeOffset>("PublishedAt");
            b.Property<decimal?>("RelevanceScore").HasPrecision(5, 4);
            b.Property<string>("Title").IsRequired().HasMaxLength(500);
            b.Property<string>("Transcript").HasColumnType("nvarchar(max)");
            b.Property<DateTimeOffset?>("UpdatedAt");
            b.Property<string>("Url").IsRequired().HasMaxLength(1000);
            b.Property<string>("WorkflowStatus").IsRequired().HasMaxLength(32);
            b.Property<string>("YouTubeVideoId").IsRequired().HasMaxLength(64);
            b.HasKey("Id");
            b.HasIndex("ChannelId");
            b.HasIndex("YouTubeVideoId").IsUnique();
        });

        modelBuilder.Entity("EngineeringDigest.Domain.Entities.Article", b =>
        {
            b.Property<Guid>("Id");
            b.Property<DateTimeOffset?>("ApprovedAt");
            b.Property<string>("ContentMarkdown").IsRequired().HasColumnType("nvarchar(max)");
            b.Property<DateTimeOffset>("CreatedAt");
            b.Property<DateTimeOffset?>("DeletedAt");
            b.Property<bool>("IsDeleted");
            b.Property<DateTimeOffset?>("PublishedAt");
            b.Property<string>("Status").IsRequired().HasMaxLength(32);
            b.Property<string>("Summary").HasMaxLength(2000);
            b.Property<string>("TelegramMessageId").HasMaxLength(128);
            b.Property<string>("Title").IsRequired().HasMaxLength(500);
            b.Property<DateTimeOffset?>("UpdatedAt");
            b.Property<Guid>("VideoId");
            b.HasKey("Id");
            b.HasIndex("VideoId").IsUnique();
        });
    }
}
