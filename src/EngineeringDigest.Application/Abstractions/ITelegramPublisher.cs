namespace EngineeringDigest.Application.Abstractions;

public interface ITelegramPublisher
{
    string CreatePreview(string title, string markdownContent);
    IReadOnlyList<string> SplitForTelegram(string title, string markdownContent);
    Task<string> PublishAsync(string title, string markdownContent, CancellationToken cancellationToken);
}
