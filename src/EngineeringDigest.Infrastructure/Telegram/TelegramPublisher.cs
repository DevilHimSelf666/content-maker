using System.Net.Http.Json;
using System.Text.Json;
using EngineeringDigest.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EngineeringDigest.Infrastructure.Telegram;

public sealed class TelegramPublisher(HttpClient httpClient, IOptions<TelegramOptions> options) : ITelegramPublisher
{
    private const int TelegramMessageLimit = 4096;
    private readonly TelegramOptions _options = options.Value;

    public string CreatePreview(string title, string markdownContent) => string.Join("\n\n---\n\n", SplitForTelegram(title, markdownContent));

    public IReadOnlyList<string> SplitForTelegram(string title, string markdownContent)
    {
        var escapedTitle = Escape(title);
        var escapedContent = Escape(markdownContent);
        var full = $"*{escapedTitle}*\n\n{escapedContent}";
        if (full.Length <= TelegramMessageLimit)
        {
            return [full];
        }

        var chunks = new List<string>();
        var remaining = escapedContent;
        var header = $"*{escapedTitle}*";
        while (remaining.Length > 0)
        {
            var prefix = chunks.Count == 0 ? header + "\n\n" : string.Empty;
            var take = Math.Min(TelegramMessageLimit - prefix.Length, remaining.Length);
            var splitAt = remaining.LastIndexOf("\n\n", take - 1, take, StringComparison.Ordinal);
            if (splitAt < 512)
            {
                splitAt = take;
            }

            chunks.Add(prefix + remaining[..splitAt].Trim());
            remaining = remaining[splitAt..].TrimStart();
        }

        return chunks;
    }

    public async Task<string> PublishAsync(string title, string markdownContent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken) || string.IsNullOrWhiteSpace(_options.ChatId))
        {
            throw new InvalidOperationException("Telegram BotToken and ChatId must be configured before manual publishing.");
        }

        var messageIds = new List<string>();
        foreach (var message in SplitForTelegram(title, markdownContent))
        {
            var response = await httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_options.BotToken}/sendMessage",
                new { chat_id = _options.ChatId, text = message, parse_mode = "MarkdownV2", disable_web_page_preview = true },
                cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            messageIds.Add(doc.RootElement.GetProperty("result").GetProperty("message_id").GetInt32().ToString());
        }

        return string.Join(',', messageIds);
    }

    private static string Escape(string value)
    {
        var chars = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
        return chars.Aggregate(value, (current, c) => current.Replace(c, "\\" + c, StringComparison.Ordinal));
    }
}
