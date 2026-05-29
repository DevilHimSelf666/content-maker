namespace EngineeringDigest.Application.Videos;

public sealed record DiscoveredVideo(
    string YouTubeVideoId,
    string Title,
    string? Description,
    string Url,
    DateTimeOffset PublishedAt);
