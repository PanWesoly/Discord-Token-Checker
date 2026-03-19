using System.IO;
using System.Text;
using DiscordTokenChecker.Models;

namespace DiscordTokenChecker.Services;

public static class ResultExporter
{
    public static void ExportHits(string filePath, IEnumerable<TokenResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Discord Token Checker Results === [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
        sb.AppendLine();

        foreach (var r in results.Where(r => r.Status == CheckStatus.Valid))
        {
            sb.AppendLine($"{r.Token} | {r.Username} | {r.Email} | {r.Phone} | Nitro:{r.NitroType} | 2FA:{r.TwoFA} | Payment:{r.PaymentInfo}");
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public static void ExportAll(string filePath, IEnumerable<TokenResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Discord Token Checker - Full Report === [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
        sb.AppendLine();

        foreach (var r in results)
        {
            sb.AppendLine($"[{r.Status}] {r.Token} | {r.Username} | {r.Email} | {r.Phone} | Nitro:{r.NitroType} | 2FA:{r.TwoFA} | Payment:{r.PaymentInfo}");
        }

        File.WriteAllText(filePath, sb.ToString());
    }
}
