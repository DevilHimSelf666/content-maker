namespace EngineeringDigest.Application.Messaging;

public sealed record DiscoverVideos;
public sealed record ExtractTranscript(Guid VideoId);
public sealed record ClassifyVideo(Guid VideoId);
public sealed record GenerateArticle(Guid VideoId);
public sealed record ApproveArticle(Guid ArticleId);
public sealed record PublishArticle(Guid ArticleId);
