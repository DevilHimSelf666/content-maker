using EngineeringDigest.Domain.SeedWork;

namespace EngineeringDigest.Domain.Entities;

public sealed class Channel : AuditableEntity
{
    public required string Name { get; set; }
    public required string YouTubeChannelId { get; set; }
    public required string RssFeedUrl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset? LastCheckedAt { get; set; }
    public ICollection<Video> Videos { get; set; } = new List<Video>();
}
