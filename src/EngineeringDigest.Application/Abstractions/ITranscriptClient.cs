namespace EngineeringDigest.Application.Abstractions;

public interface ITranscriptClient
{
    Task<string> GetTranscriptAsync(string youtubeVideoId, CancellationToken cancellationToken);
}
