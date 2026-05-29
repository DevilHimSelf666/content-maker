namespace EngineeringDigest.Application.Abstractions;

public interface ITelegramPublisher
{
    Task<string> PublishAsync(string title, string markdownContent, CancellationToken cancellationToken);
}
