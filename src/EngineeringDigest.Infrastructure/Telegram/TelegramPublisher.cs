using System.Net.Http.Json;
using System.Text.Json;
using EngineeringDigest.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EngineeringDigest.Infrastructure.Telegram;

public sealed class TelegramPublisher(HttpClient httpClient, IOptions<TelegramOptions> options) : ITelegramPublisher
{
    private readonly TelegramOptions _options = options.Value;

    public async Task<string> PublishAsync(string title, string markdownContent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken) || string.IsNullOrWhiteSpace(_options.ChatId))
        {
            throw new InvalidOperationException("Telegram BotToken and ChatId must be configured before manual publishing.");
        }

        var message = $"*{Escape(title)}*\n\n{Escape(markdownContent)}";
        var response = await httpClient.PostAsJsonAsync(
            $"https://api.telegram.org/bot{_options.BotToken}/sendMessage",
            new { chat_id = _options.ChatId, text = message, parse_mode = "MarkdownV2" },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return doc.RootElement.GetProperty("result").GetProperty("message_id").GetInt32().ToString();
    }

    private static string Escape(string value)
    {
        var chars = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
        return chars.Aggregate(value, (current, c) => current.Replace(c, "\\" + c, StringComparison.Ordinal));
    }
}
