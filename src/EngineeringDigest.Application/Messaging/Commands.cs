namespace EngineeringDigest.Application.Messaging;

public sealed record DiscoverVideos;
public sealed record ExtractTranscript(Guid VideoId);
public sealed record ClassifyVideo(Guid VideoId);
public sealed record GenerateArticle(Guid VideoId);
public sealed record ApproveArticle(Guid ArticleId);
public sealed record PublishArticle(Guid ArticleId);

public sealed record PromoteApprovedArticleToKnowledge(Guid ArticleId);
public sealed record RebuildKnowledgeEmbeddings(int BatchSize = 25);
public sealed record ReindexKnowledge;
public sealed record GenerateWeeklyKnowledgeDigest(DateOnly WeekStart);
public sealed record AnalyzeKnowledgeArticleQuality(int BatchSize = 25);
