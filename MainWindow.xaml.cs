using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using DiscordTokenChecker.Models;
using DiscordTokenChecker.Services;

namespace DiscordTokenChecker;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TokenResult> _allResults = new();
    private readonly Dictionary<CheckStatus, ObservableCollection<TokenResult>> _resultsByStatus = new()
    {
        [CheckStatus.Valid]   = new(),
        [CheckStatus.Invalid] = new(),
    };
    private readonly ObservableCollection<TokenResult> _errorResults = new();

    private readonly CheckerEngine       _engine   = new();
    private readonly NotificationService _notifier = new();

    private List<string> _tokens  = new();
    private List<string> _proxies = new();
    private Button[]     _tabButtons = Array.Empty<Button>();
    private SettingsWindow? _settingsWindow;

    public MainWindow()
    {
        InitializeComponent();
        dgResults.ItemsSource = _allResults;
        _tabButtons = new[] { tabAll, tabValid, tabInvalid, tabErrors };

        AppSettings.Instance.PropertyChanged += (_, e) => ApplyColumnVisibility();
        ApplyColumnVisibility();

        sliderThreads.ValueChanged += (_, e) =>
            txtThreads.Text = ((int)e.NewValue).ToString();

        _engine.OnResult += result =>
        {
            result.CreatedAt = SnowflakeToDate(result.Id);

            Dispatcher.Invoke(() =>
            {
                _allResults.Add(result);
                if (_resultsByStatus.TryGetValue(result.Status, out var bucket))
                    bucket.Add(result);
                else
                    _errorResults.Add(result);
            });

            if (result.Status == CheckStatus.Valid)
                _ = _notifier.SendValidHitAsync(result);
        };

        _engine.OnStatsUpdate += (valid, invalid, errors, checked_, cpm) =>
        {
            Dispatcher.Invoke(() =>
            {
                txtValid.Text    = valid.ToString();
                txtInvalid.Text  = invalid.ToString();
                txtErrors.Text   = errors.ToString();
                txtCPM.Text      = ((int)cpm).ToString();
                txtProgress.Text = $"{checked_} / {_tokens.Count}";
                if (_tokens.Count > 0)
                    progressBar.Value = (double)checked_ / _tokens.Count * 100;
            });
        };

        _engine.OnComplete += () =>
        {
            Dispatcher.Invoke(() =>
            {
                SetControlsEnabled(checking: false);
                MessageBox.Show(
                    $"Checking complete!\n\nValid: {txtValid.Text}\nInvalid: {txtInvalid.Text}\nErrors: {txtErrors.Text}",
                    "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        };
    }

    // ── Snowflake → account creation date ────────────────────────────────
    private static string SnowflakeToDate(string snowflake)
    {
        if (!ulong.TryParse(snowflake, out var id) || id == 0)
            return "";
        var timestampMs = (id >> 22) + 1420070400000UL;
        var dt = DateTimeOffset.FromUnixTimeMilliseconds((long)timestampMs).UtcDateTime;
        return dt.ToString("dd.MM.yyyy HH:mm");
    }

    // ── Settings window ───────────────────────────────────────────────────
    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is { IsVisible: true }) { _settingsWindow.Activate(); return; }
        _settingsWindow = new SettingsWindow { Owner = this };
        _settingsWindow.Show();
    }

    // ── Column visibility ─────────────────────────────────────────────────
    private void ApplyColumnVisibility()
    {
        Dispatcher.Invoke(() =>
        {
            var s = AppSettings.Instance;
            var map = new Dictionary<string, bool>
            {
                ["Status"]      = s.ShowStatus,
                ["Username"]    = s.ShowUsername,
                ["Email"]       = s.ShowEmail,
                ["Phone"]       = s.ShowPhone,
                ["Nitro"]       = s.ShowNitro,
                ["Nitro Since"] = s.ShowNitroSince,
                ["2FA"]         = s.ShowTwoFA,
                ["Locale"]      = s.ShowLocale,
                ["Verified"]    = s.ShowVerified,
                ["Connected"]   = s.ShowConnected,
                ["Payment"]     = s.ShowPayment,
                ["Token"]       = s.ShowToken,
                ["Created"]     = s.ShowCreated,
            };
            foreach (var col in dgResults.Columns)
                if (col.Header is string h && map.TryGetValue(h, out var vis))
                    col.Visibility = vis ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    // ── Tabs ──────────────────────────────────────────────────────────────
    private void SetActiveTab(Button activeBtn, ObservableCollection<TokenResult> source)
    {
        foreach (var btn in _tabButtons)
            btn.Style = (Style)FindResource("TabButton");
        activeBtn.Style = (Style)FindResource("TabButtonActive");
        dgResults.ItemsSource = source;
    }

    private void TabAll_Click(object sender, RoutedEventArgs e)     => SetActiveTab(tabAll,     _allResults);
    private void TabValid_Click(object sender, RoutedEventArgs e)   => SetActiveTab(tabValid,   _resultsByStatus[CheckStatus.Valid]);
    private void TabInvalid_Click(object sender, RoutedEventArgs e) => SetActiveTab(tabInvalid, _resultsByStatus[CheckStatus.Invalid]);
    private void TabErrors_Click(object sender, RoutedEventArgs e)  => SetActiveTab(tabErrors,  _errorResults);

    // ── File loading ──────────────────────────────────────────────────────
    private static List<string>? PickFile(string title)
    {
        var dlg = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", Title = title };
        return dlg.ShowDialog() == true
            ? File.ReadAllLines(dlg.FileName).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList()
            : null;
    }

    private void BtnLoadTokens_Click(object sender, RoutedEventArgs e)
    {
        var lines = PickFile("Load Token List");
        if (lines is null) return;
        _tokens = lines;
        txtTokenCount.Text = _tokens.Count.ToString();
        progressBar.Value  = 0;
        txtProgress.Text   = $"0 / {_tokens.Count}";
    }

    private void BtnLoadProxies_Click(object sender, RoutedEventArgs e)
    {
        var lines = PickFile("Load Proxy List (IP:PORT or IP:PORT:USER:PASS)");
        if (lines is null) return;
        _proxies = lines.Where(l => l.Contains(':')).ToList();
        txtProxyCount.Text = _proxies.Count.ToString();
        txtProxyMode.Text  = _proxies.Count > 0 ? "Rotating" : "Proxyless";
    }

    // ── Start / Stop ──────────────────────────────────────────────────────
    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (_tokens.Count == 0)
        {
            MessageBox.Show("Please load tokens first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _allResults.Clear();
        _errorResults.Clear();
        foreach (var col in _resultsByStatus.Values) col.Clear();

        SetControlsEnabled(checking: true);
        progressBar.Value = 0;

        _notifier.DiscordWebhookUrl = txtWebhook.Text.Trim();
        _notifier.TelegramBotToken  = txtTgToken.Text.Trim();
        _notifier.TelegramChatId    = txtTgChatId.Text.Trim();

        _engine.SetProxies(_proxies);
        await _engine.StartAsync(_tokens, (int)sliderThreads.Value);
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _engine.Stop();
        SetControlsEnabled(checking: false);
    }

    // ── Export ────────────────────────────────────────────────────────────
    private void BtnExportHits_Click(object sender, RoutedEventArgs e)
    {
        var path = PickSavePath($"hits_{DateTime.Now:yyyyMMdd_HHmmss}.txt", "Export Valid Tokens");
        if (path is null) return;
        ResultExporter.ExportHits(path, _allResults);
        MessageBox.Show($"Exported {_resultsByStatus[CheckStatus.Valid].Count} hits!", "Export",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnExportAll_Click(object sender, RoutedEventArgs e)
    {
        var path = PickSavePath($"results_{DateTime.Now:yyyyMMdd_HHmmss}.txt", "Export All Results");
        if (path is null) return;
        ResultExporter.ExportAll(path, _allResults);
        MessageBox.Show($"Exported {_allResults.Count} results!", "Export",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string? PickSavePath(string defaultName, string title)
    {
        var dlg = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt", FileName = defaultName, Title = title };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private void SetControlsEnabled(bool checking)
    {
        btnStart.IsEnabled       = !checking;
        btnStop.IsEnabled        = checking;
        btnLoadTokens.IsEnabled  = !checking;
        btnLoadProxies.IsEnabled = !checking;
    }
}
