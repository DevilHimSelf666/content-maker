using EngineeringDigest.Application.Videos;

namespace EngineeringDigest.Application.Abstractions;

public interface IYouTubeRssClient
{
    Task<IReadOnlyList<DiscoveredVideo>> GetLatestVideosAsync(string rssFeedUrl, CancellationToken cancellationToken);
}
