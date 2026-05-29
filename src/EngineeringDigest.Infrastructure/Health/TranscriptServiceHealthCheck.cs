using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using EngineeringDigest.Infrastructure.Transcripts;

namespace EngineeringDigest.Infrastructure.Health;

public sealed class TranscriptServiceHealthCheck(HttpClient httpClient, IOptions<TranscriptOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
            using var response = await httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode ? HealthCheckResult.Healthy("Transcript service is reachable.") : HealthCheckResult.Unhealthy($"Transcript service returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Transcript service health check failed.", ex);
        }
    }
}
