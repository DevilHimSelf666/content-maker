using System.ServiceModel.Syndication;
using System.Xml;
using EngineeringDigest.Application.Abstractions;
using EngineeringDigest.Application.Videos;

namespace EngineeringDigest.Infrastructure.YouTube;

public sealed class YouTubeRssClient(HttpClient httpClient) : IYouTubeRssClient
{
    public async Task<IReadOnlyList<DiscoveredVideo>> GetLatestVideosAsync(string rssFeedUrl, CancellationToken cancellationToken)
    {
        await using var stream = await httpClient.GetStreamAsync(rssFeedUrl, cancellationToken);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
        var feed = SyndicationFeed.Load(reader);
        var items = feed?.Items ?? Enumerable.Empty<SyndicationItem>();

        return items.Select(item =>
        {
            var rawId = string.IsNullOrWhiteSpace(item.Id) ? item.Links.FirstOrDefault()?.Uri.Query ?? string.Empty : item.Id;
            var id = rawId.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? rawId;
            var url = item.Links.FirstOrDefault()?.Uri.ToString() ?? $"https://www.youtube.com/watch?v={id}";
            return new DiscoveredVideo(
                id,
                item.Title?.Text ?? "Untitled YouTube video",
                item.Summary?.Text,
                url,
                item.PublishDate == default ? DateTimeOffset.UtcNow : item.PublishDate);
        }).ToArray();
    }
}
