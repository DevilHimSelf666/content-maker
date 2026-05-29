namespace EngineeringDigest.Application.Messaging;

public sealed record DiscoverVideos;
public sealed record ExtractTranscript(Guid VideoId);
public sealed record ClassifyVideo(Guid VideoId);
public sealed record GenerateArticle(Guid VideoId);
public sealed record RegenerateArticle(Guid ArticleId, int? PromptVersion = null);
public sealed record ApproveArticle(Guid ArticleId, string? Actor = null);
public sealed record RejectArticle(Guid ArticleId, string? Actor = null);
public sealed record PublishArticle(Guid ArticleId, string? Actor = null);
public sealed record CleanupAbandonedJobs;
public sealed record CleanupFailedTransientRecords;
public sealed record RefreshChannelMetadata;
