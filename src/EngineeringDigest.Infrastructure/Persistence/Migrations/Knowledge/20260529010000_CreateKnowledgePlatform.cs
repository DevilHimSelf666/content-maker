using EngineeringDigest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringDigest.Infrastructure.Persistence.Migrations.Knowledge;

[DbContext(typeof(KnowledgeDbContext))]
[Migration("20260529010000_CreateKnowledgePlatform")]
public partial class CreateKnowledgePlatform : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS "KnowledgeCategories" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "Name" character varying(128) NOT NULL,
    "Description" character varying(1000) NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_KnowledgeCategories_Name" ON "KnowledgeCategories" ("Name");

CREATE TABLE IF NOT EXISTS "KnowledgeTags" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "Name" character varying(128) NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_KnowledgeTags_Name" ON "KnowledgeTags" ("Name");

CREATE TABLE IF NOT EXISTS "LearningPaths" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_LearningPaths_Name" ON "LearningPaths" ("Name");

CREATE TABLE IF NOT EXISTS "KnowledgeArticles" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "ArticleId" uuid NOT NULL,
    "CategoryId" uuid NULL REFERENCES "KnowledgeCategories" ("Id") ON DELETE SET NULL,
    "Title" character varying(500) NOT NULL,
    "BodyMarkdown" text NOT NULL,
    "Summary" character varying(2000) NULL,
    "KeyTakeaways" text NOT NULL,
    "Version" integer NOT NULL,
    "ViewCount" integer NOT NULL,
    "UsefulCount" integer NOT NULL,
    "QualityTechnicalDepth" numeric(5,4) NULL,
    "QualityClarity" numeric(5,4) NULL,
    "QualityRelevance" numeric(5,4) NULL,
    "QualityPracticalValue" numeric(5,4) NULL,
    "QualityNotes" character varying(2000) NULL,
    "QualityEvaluatedAt" timestamp with time zone NULL,
    "EmbeddingsUpdatedAt" timestamp with time zone NULL,
    "TitleEmbedding" vector(1536) NULL,
    "BodyEmbedding" vector(1536) NULL,
    "KeyTakeawaysEmbedding" vector(1536) NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_KnowledgeArticles_ArticleId" ON "KnowledgeArticles" ("ArticleId");
CREATE INDEX IF NOT EXISTS "IX_KnowledgeArticles_CategoryId" ON "KnowledgeArticles" ("CategoryId");
CREATE INDEX IF NOT EXISTS "IX_KnowledgeArticles_BodyEmbedding" ON "KnowledgeArticles" USING hnsw ("BodyEmbedding" vector_cosine_ops);

CREATE TABLE IF NOT EXISTS "KnowledgeArticleTag" (
    "KnowledgeArticleId" uuid NOT NULL REFERENCES "KnowledgeArticles" ("Id") ON DELETE CASCADE,
    "KnowledgeTagId" uuid NOT NULL REFERENCES "KnowledgeTags" ("Id") ON DELETE CASCADE,
    PRIMARY KEY ("KnowledgeArticleId", "KnowledgeTagId")
);

CREATE TABLE IF NOT EXISTS "KnowledgeReferences" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "KnowledgeArticleId" uuid NOT NULL REFERENCES "KnowledgeArticles" ("Id") ON DELETE CASCADE,
    "Title" character varying(500) NOT NULL,
    "Url" character varying(1000) NOT NULL
);

CREATE TABLE IF NOT EXISTS "KnowledgeArticleRelations" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "SourceArticleId" uuid NOT NULL REFERENCES "KnowledgeArticles" ("Id") ON DELETE RESTRICT,
    "RelatedArticleId" uuid NOT NULL REFERENCES "KnowledgeArticles" ("Id") ON DELETE RESTRICT,
    "Score" numeric(5,4) NOT NULL,
    "Reason" character varying(500) NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_KnowledgeArticleRelations_SourceArticleId_RelatedArticleId" ON "KnowledgeArticleRelations" ("SourceArticleId", "RelatedArticleId");

CREATE TABLE IF NOT EXISTS "LearningPathArticle" (
    "LearningPathId" uuid NOT NULL REFERENCES "LearningPaths" ("Id") ON DELETE CASCADE,
    "KnowledgeArticleId" uuid NOT NULL REFERENCES "KnowledgeArticles" ("Id") ON DELETE CASCADE,
    "Order" integer NOT NULL,
    PRIMARY KEY ("LearningPathId", "KnowledgeArticleId")
);

CREATE TABLE IF NOT EXISTS "WeeklyDigests" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "WeekStart" date NOT NULL,
    "Title" character varying(500) NOT NULL,
    "ContentMarkdown" text NOT NULL,
    "TrendingTopics" character varying(2000) NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_WeeklyDigests_WeekStart" ON "WeeklyDigests" ("WeekStart");

CREATE TABLE IF NOT EXISTS "KnowledgeSearchLogs" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "Query" character varying(1000) NOT NULL,
    "SearchType" character varying(32) NOT NULL,
    "ResultCount" integer NOT NULL
);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS "KnowledgeSearchLogs";
DROP TABLE IF EXISTS "WeeklyDigests";
DROP TABLE IF EXISTS "LearningPathArticle";
DROP TABLE IF EXISTS "KnowledgeArticleRelations";
DROP TABLE IF EXISTS "KnowledgeReferences";
DROP TABLE IF EXISTS "KnowledgeArticleTag";
DROP TABLE IF EXISTS "KnowledgeArticles";
DROP TABLE IF EXISTS "LearningPaths";
DROP TABLE IF EXISTS "KnowledgeTags";
DROP TABLE IF EXISTS "KnowledgeCategories";
""");
    }
}
