using System.Net;
using System.Net.Http;
using System.Text.Json;
using DiscordTokenChecker.Models;

namespace DiscordTokenChecker.Services;

public class DiscordChecker
{
    private const string SuperProperties =
        "eyJvcyI6IldpbmRvd3MiLCJicm93c2VyIjoiQ2hyb21lIiwiZGV2aWNlIjoiIiwic3lzdGVtX2xvY2FsZSI6InBsLVBMIiwiYnJvd3Nlcl91c2VyX2FnZW50IjoiTW96aWxsYS81LjAgKFdpbmRvd3MgTlQgMTAuMDsgV2luNjQ7IHg2NCkgQXBwbGVXZWJLaXQvNTM3LjM2IChLSFRNTCwgbGlrZSBHZWNrbykgQ2hyb21lLzEyNC4wLjYzNjcuMTE4IFNhZmFyaS81MzcuMzYiLCJicm93c2VyX3ZlcnNpb24iOiIxMjQuMC42MzY3LjExOCIsIm9zX3ZlcnNpb24iOiIxMCIsInJlZmVycmVyIjoiaHR0cHM6Ly93d3cuZ29vZ2xlLmNvbS8iLCJyZWZlcnJpbmdfZG9tYWluIjoid3d3Lmdvb2dsZS5jb20iLCJzZWFyY2hfZW5naW5lIjoiZ29vZ2xlIiwicmVmZXJyZXJfY3VycmVudCI6Imh0dHBzOi8vd3d3Lmdvb2dsZS5jb20vIiwicmVmZXJyaW5nX2RvbWFpbl9jdXJyZW50Ijoid3d3Lmdvb2dsZS5jb20iLCJzZWFyY2hfZW5naW5lX2N1cnJlbnQiOiJnb29nbGUiLCJyZWxlYXNlX2NoYW5uZWwiOiJzdGFibGUiLCJjbGllbnRfYnVpbGRfbnVtYmVyIjoyOTE5NjMsImNsaWVudF9ldmVudF9zb3VyY2UiOm51bGwsImRlc2lnbl9pZCI6MH0=";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.6367.118 Safari/537.36";

    // Proxyless client
    private static readonly HttpClient _proxylessClient = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private List<string> _proxies = new();
    private int _proxyIndex;
    private readonly object _proxyLock = new();

    public void SetProxies(List<string> proxies)
    {
        _proxies = proxies;
        _proxyIndex = 0;
    }

    private HttpClient GetClient()
    {
        if (_proxies.Count == 0)
            return _proxylessClient;

        string proxyStr;
        lock (_proxyLock)
        {
            proxyStr = _proxies[_proxyIndex % _proxies.Count];
            _proxyIndex++;
        }

        var parts = proxyStr.Split(':');
        WebProxy proxy;
        if (parts.Length >= 4)
        {
            // IP:PORT:USER:PASS
            proxy = new WebProxy($"http://{parts[0]}:{parts[1]}")
            {
                Credentials = new NetworkCredential(parts[2], parts[3])
            };
        }
        else if (parts.Length >= 2)
        {
            // IP:PORT
            proxy = new WebProxy($"http://{parts[0]}:{parts[1]}");
        }
        else
        {
            return _proxylessClient;
        }

        var handler = new HttpClientHandler
        {
            Proxy = proxy,
            UseProxy = true,
            AutomaticDecompression = DecompressionMethods.All
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
    }

    private HttpRequestMessage BuildRequest(string url, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("Authorization", token);
        req.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        req.Headers.TryAddWithoutValidation("X-Super-Properties", SuperProperties);
        req.Headers.TryAddWithoutValidation("X-Debug-Options", "bugReporterEnabled");
        req.Headers.TryAddWithoutValidation("X-Discord-Timezone", "Europe/Warsaw");
        req.Headers.TryAddWithoutValidation("X-Discord-Locale", "pl");
        req.Headers.TryAddWithoutValidation("Sec-Ch-Ua", "\"Not-A.Brand\";v=\"99\", \"Chromium\";v=\"124\"");
        req.Headers.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", "?0");
        req.Headers.TryAddWithoutValidation("Sec-Ch-Ua-Platform", "\"Windows\"");
        req.Headers.TryAddWithoutValidation("Accept", "*/*");
        req.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
        req.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
        req.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
        req.Headers.TryAddWithoutValidation("Referer", "https://discord.com/channels/@me");
        req.Headers.TryAddWithoutValidation("Accept-Language", "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7");
        req.Headers.TryAddWithoutValidation("Priority", "u=1, i");
        return req;
    }

    public async Task<TokenResult> CheckTokenAsync(string token, CancellationToken ct = default)
    {
        var result = new TokenResult { Token = token, Status = CheckStatus.Checking };
        var client = GetClient();
        var isProxy = client != _proxylessClient;

        try
        {
            // Step 1: Validate token
            using var r1 = await client.SendAsync(
                BuildRequest("https://discord.com/api/v9/users/@me/affinities/users", token), ct);
            var body1 = await r1.Content.ReadAsStringAsync(ct);

            if (body1.Contains("Unauthorized"))
            {
                result.Status = CheckStatus.Invalid;
                return result;
            }

            if (!body1.Contains("user_affinities"))
            {
                result.Status = CheckStatus.Invalid;
                return result;
            }

            result.Status = CheckStatus.Valid;

            // Step 2: Get user info
            using var r2 = await client.SendAsync(
                BuildRequest("https://discord.com/api/v9/users/@me", token), ct);
            var body2 = await r2.Content.ReadAsStringAsync(ct);

            try
            {
                using var doc2 = JsonDocument.Parse(body2);
                var root = doc2.RootElement;
                result.Id = GetStr(root, "id");
                result.Email = GetStr(root, "email");
                result.Locale = GetStr(root, "locale");
                result.Verified = root.TryGetProperty("verified", out var v) && v.GetBoolean();
                result.Phone = GetStr(root, "phone");

                if (root.TryGetProperty("authenticator_types", out var authTypes))
                    result.TwoFA = authTypes.GetArrayLength() > 0 ? "Yes" : "No";
                else
                    result.TwoFA = "No";
            }
            catch { }

            // Step 3: Get profile
            if (!string.IsNullOrEmpty(result.Id))
            {
                using var r3 = await client.SendAsync(
                    BuildRequest($"https://discord.com/api/v9/users/{result.Id}/profile?with_mutual_guilds=false&with_mutual_friends=false&with_mutual_friends_count=false", token), ct);
                var body3 = await r3.Content.ReadAsStringAsync(ct);

                try
                {
                    using var doc3 = JsonDocument.Parse(body3);
                    var root3 = doc3.RootElement;

                    if (root3.TryGetProperty("user", out var userObj))
                        result.Username = GetStr(userObj, "username");

                    if (root3.TryGetProperty("premium_since", out var ps) && ps.ValueKind == JsonValueKind.String)
                    {
                        var val = ps.GetString() ?? "";
                        result.NitroSince = val.Contains('T') ? val.Split('T')[0] : val;
                    }

                    if (root3.TryGetProperty("premium_type", out var pt))
                    {
                        result.NitroType = pt.GetInt32() switch
                        {
                            1 => "BASIC",
                            2 => "NITRO",
                            3 => "NITRO BASIC",
                            _ => "None"
                        };
                    }
                    else
                    {
                        result.NitroType = "None";
                    }

                    if (root3.TryGetProperty("connected_accounts", out var ca) && ca.ValueKind == JsonValueKind.Array)
                    {
                        var types = new List<string>();
                        foreach (var acc in ca.EnumerateArray())
                        {
                            if (acc.TryGetProperty("type", out var t))
                                types.Add(t.GetString() ?? "");
                        }
                        result.ConnectedAccounts = string.Join(", ", types);
                    }
                }
                catch { }
            }

            // Step 4: Payment sources
            using var r4 = await client.SendAsync(
                BuildRequest("https://discord.com/api/v9/users/@me/billing/payment-sources", token), ct);
            var body4 = await r4.Content.ReadAsStringAsync(ct);

            try
            {
                using var doc4 = JsonDocument.Parse(body4);
                if (doc4.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var paymentParts = new List<string>();
                    foreach (var src in doc4.RootElement.EnumerateArray())
                    {
                        if (src.TryGetProperty("brand", out var brand))
                        {
                            var last4 = GetStr(src, "last_4");
                            var expM = src.TryGetProperty("expires_month", out var em) ? em.GetInt32().ToString() : "?";
                            var expY = src.TryGetProperty("expires_year", out var ey) ? ey.GetInt32().ToString() : "?";
                            paymentParts.Add($"{brand.GetString()} *{last4} ({expM}/{expY})");
                        }
                        if (src.TryGetProperty("email", out var ppEmail))
                            paymentParts.Add($"PayPal:{ppEmail.GetString()}");
                    }
                    result.PaymentInfo = paymentParts.Count > 0 ? string.Join(" | ", paymentParts) : "None";
                }
            }
            catch { result.PaymentInfo = "None"; }

            // Step 5: Nitro entitlements
            using var r5 = await client.SendAsync(
                BuildRequest("https://discord.com/api/v9/users/@me/applications/521842831262875670/entitlements?exclude_consumed=true", token), ct);
            var body5 = await r5.Content.ReadAsStringAsync(ct);

            try
            {
                using var doc5 = JsonDocument.Parse(body5);
                if (doc5.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var names = new List<string>();
                    foreach (var ent in doc5.RootElement.EnumerateArray())
                    {
                        if (ent.TryGetProperty("name", out var n))
                            names.Add(n.GetString() ?? "");
                    }
                    result.NitroBalance = names.Count > 0 ? string.Join(", ", names) : "None";
                }
            }
            catch { result.NitroBalance = "None"; }
        }
        catch (TaskCanceledException) { throw; }
        catch (Exception)
        {
            if (result.Status == CheckStatus.Checking)
                result.Status = CheckStatus.Error;
        }
        finally
        {
            if (isProxy) client.Dispose();
        }

        return result;
    }

    private static string GetStr(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v))
        {
            if (v.ValueKind == JsonValueKind.String)
                return v.GetString() ?? "";
            if (v.ValueKind == JsonValueKind.Null)
                return "";
            return v.ToString();
        }
        return "";
    }
}
