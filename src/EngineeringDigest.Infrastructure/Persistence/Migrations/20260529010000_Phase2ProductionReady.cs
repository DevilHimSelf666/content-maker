using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringDigest.Infrastructure.Persistence.Migrations;

public partial class Phase2ProductionReady : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("ApprovedBy", "Articles", "nvarchar(256)", maxLength: 256, nullable: true);
        migrationBuilder.AddColumn<decimal>("PracticalValueScore", "Articles", "decimal(4,2)", precision: 4, scale: 2, nullable: true);
        migrationBuilder.AddColumn<int>("PromptVersion", "Articles", "int", nullable: true);
        migrationBuilder.AddColumn<decimal>("QualityScore", "Articles", "decimal(4,2)", precision: 4, scale: 2, nullable: true);
        migrationBuilder.AddColumn<decimal>("ReadabilityScore", "Articles", "decimal(4,2)", precision: 4, scale: 2, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("RejectedAt", "Articles", "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>("RejectedBy", "Articles", "nvarchar(256)", maxLength: 256, nullable: true);
        migrationBuilder.AddColumn<decimal>("RelevanceScore", "Articles", "decimal(4,2)", precision: 4, scale: 2, nullable: true);
        migrationBuilder.AddColumn<decimal>("TechnicalDepthScore", "Articles", "decimal(4,2)", precision: 4, scale: 2, nullable: true);
        migrationBuilder.AddColumn<string>("PublishedBy", "Articles", "nvarchar(256)", maxLength: 256, nullable: true);

        migrationBuilder.CreateTable(
            name: "PromptTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Template = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PromptTemplates", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ArticleVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionNumber = table.Column<int>(type: "int", nullable: false),
                GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                PromptVersion = table.Column<int>(type: "int", nullable: true),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                QualityScore = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ArticleVersions", x => x.Id);
                table.ForeignKey("FK_ArticleVersions_Articles_ArticleId", x => x.ArticleId, "Articles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProcessingJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JobType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                VideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                FailureReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcessingJobs", x => x.Id);
                table.ForeignKey("FK_ProcessingJobs_Articles_ArticleId", x => x.ArticleId, "Articles", "Id");
                table.ForeignKey("FK_ProcessingJobs_Videos_VideoId", x => x.VideoId, "Videos", "Id");
            });

        migrationBuilder.CreateTable(
            name: "AuditTrails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EntityName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AuditTrails", x => x.Id));

        migrationBuilder.CreateIndex("IX_PromptTemplates_Kind_Version", "PromptTemplates", new[] { "Kind", "Version" }, unique: true);
        migrationBuilder.CreateIndex("IX_ArticleVersions_ArticleId_VersionNumber", "ArticleVersions", new[] { "ArticleId", "VersionNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProcessingJobs_ArticleId", "ProcessingJobs", "ArticleId");
        migrationBuilder.CreateIndex("IX_ProcessingJobs_VideoId", "ProcessingJobs", "VideoId");
        migrationBuilder.CreateIndex("IX_ProcessingJobs_JobType_VideoId_ArticleId_Status", "ProcessingJobs", new[] { "JobType", "VideoId", "ArticleId", "Status" });
        migrationBuilder.CreateIndex("IX_AuditTrails_EntityName_EntityId_OccurredAt", "AuditTrails", new[] { "EntityName", "EntityId", "OccurredAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AuditTrails");
        migrationBuilder.DropTable("ArticleVersions");
        migrationBuilder.DropTable("ProcessingJobs");
        migrationBuilder.DropTable("PromptTemplates");
        migrationBuilder.DropColumn("ApprovedBy", "Articles");
        migrationBuilder.DropColumn("PracticalValueScore", "Articles");
        migrationBuilder.DropColumn("PromptVersion", "Articles");
        migrationBuilder.DropColumn("QualityScore", "Articles");
        migrationBuilder.DropColumn("ReadabilityScore", "Articles");
        migrationBuilder.DropColumn("RejectedAt", "Articles");
        migrationBuilder.DropColumn("RejectedBy", "Articles");
        migrationBuilder.DropColumn("RelevanceScore", "Articles");
        migrationBuilder.DropColumn("TechnicalDepthScore", "Articles");
        migrationBuilder.DropColumn("PublishedBy", "Articles");
    }
}
