using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EngineeringDigest.Application.Knowledge;
using Microsoft.Extensions.Options;

namespace EngineeringDigest.Infrastructure.Knowledge;

public sealed class OpenAiCompatibleEmbeddingProvider(HttpClient httpClient, IOptions<EmbeddingOptions> options) : IEmbeddingProvider
{
    private readonly EmbeddingOptions _options = options.Value;

    public async Task<float[]> GenerateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("Embedding:Model must be configured. Embedding models are never hardcoded.");
        }

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        var response = await httpClient.PostAsJsonAsync("/embeddings", new
        {
            model = _options.Model,
            input = request.Text,
            dimensions = _options.Dimensions > 0 ? _options.Dimensions : null as int?
        }, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return doc.RootElement.GetProperty("data")[0].GetProperty("embedding")
            .EnumerateArray()
            .Select(x => x.GetSingle())
            .ToArray();
    }
}
