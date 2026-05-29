using System.Net.Http.Headers;
using EngineeringDigest.Infrastructure.Llm;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EngineeringDigest.Infrastructure.Health;

public sealed class LlmProviderHealthCheck(HttpClient httpClient, IOptions<LlmOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = options.Value;
            if (string.IsNullOrWhiteSpace(value.Model))
            {
                return HealthCheckResult.Degraded("LLM model is not configured.");
            }

            httpClient.BaseAddress = new Uri(value.BaseUrl.TrimEnd('/'));
            if (!string.IsNullOrWhiteSpace(value.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value.ApiKey);
            }

            using var response = await httpClient.GetAsync("/models", cancellationToken);
            return response.IsSuccessStatusCode ? HealthCheckResult.Healthy("LLM provider is reachable.") : HealthCheckResult.Degraded($"LLM provider returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("LLM provider health check failed.", ex);
        }
    }
}
