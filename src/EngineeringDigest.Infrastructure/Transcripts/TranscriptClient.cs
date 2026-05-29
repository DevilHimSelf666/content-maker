using System.Net.Http.Json;
using EngineeringDigest.Application.Abstractions;

namespace EngineeringDigest.Infrastructure.Transcripts;

public sealed class TranscriptClient(HttpClient httpClient) : ITranscriptClient
{
    public async Task<string> GetTranscriptAsync(string youtubeVideoId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/transcripts", new TranscriptRequest(youtubeVideoId), cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TranscriptResponse>(cancellationToken);
        return payload?.Text ?? string.Empty;
    }

    private sealed record TranscriptRequest(string YouTubeVideoId);
    private sealed record TranscriptResponse(string YouTubeVideoId, string Text);
}
