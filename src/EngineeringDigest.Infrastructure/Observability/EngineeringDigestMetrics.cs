using System.Diagnostics.Metrics;

namespace EngineeringDigest.Infrastructure.Observability;

public sealed class EngineeringDigestMetrics
{
    public const string MeterName = "EngineeringDigest";
    private readonly Counter<long> _videosDiscovered;
    private readonly Counter<long> _articlesGenerated;
    private readonly Counter<long> _articlesApproved;
    private readonly Counter<long> _articlesPublished;
    private readonly Counter<long> _failedJobs;
    private readonly Histogram<double> _articleGenerationTime;

    public EngineeringDigestMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _videosDiscovered = meter.CreateCounter<long>("engineering_digest_videos_discovered");
        _articlesGenerated = meter.CreateCounter<long>("engineering_digest_articles_generated");
        _articlesApproved = meter.CreateCounter<long>("engineering_digest_articles_approved");
        _articlesPublished = meter.CreateCounter<long>("engineering_digest_articles_published");
        _failedJobs = meter.CreateCounter<long>("engineering_digest_failed_jobs");
        _articleGenerationTime = meter.CreateHistogram<double>("engineering_digest_article_generation_duration_ms");
    }

    public void VideoDiscovered() => _videosDiscovered.Add(1);
    public void ArticleGenerated(double durationMilliseconds) { _articlesGenerated.Add(1); _articleGenerationTime.Record(durationMilliseconds); }
    public void ArticleApproved() => _articlesApproved.Add(1);
    public void ArticlePublished() => _articlesPublished.Add(1);
    public void JobFailed() => _failedJobs.Add(1);
}
