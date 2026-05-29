using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringDigest.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Channels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                YouTubeChannelId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                RssFeedUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                LastCheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Channels", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Videos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                YouTubeVideoId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkflowStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Transcript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsRelevant = table.Column<bool>(type: "bit", nullable: true),
                ClassificationReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                RelevanceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Videos", x => x.Id);
                table.ForeignKey("FK_Videos_Channels_ChannelId", x => x.ChannelId, "Channels", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Articles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                ContentMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                TelegramMessageId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Articles", x => x.Id);
                table.ForeignKey("FK_Articles_Videos_VideoId", x => x.VideoId, "Videos", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Channels_YouTubeChannelId", "Channels", "YouTubeChannelId", unique: true);
        migrationBuilder.CreateIndex("IX_Videos_ChannelId", "Videos", "ChannelId");
        migrationBuilder.CreateIndex("IX_Videos_YouTubeVideoId", "Videos", "YouTubeVideoId", unique: true);
        migrationBuilder.CreateIndex("IX_Articles_VideoId", "Articles", "VideoId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Articles");
        migrationBuilder.DropTable("Videos");
        migrationBuilder.DropTable("Channels");
    }
}
