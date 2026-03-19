using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiscordTokenChecker.Models;

/// <summary>
/// Singleton z ustawieniami aplikacji. Zmiany propagują się przez PropertyChanged
/// — MainWindow nasłuchuje i na bieżąco ukrywa/pokazuje kolumny.
/// </summary>
public sealed class AppSettings : INotifyPropertyChanged
{
    public static readonly AppSettings Instance = new();
    private AppSettings() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Kolumny DataGrid ─────────────────────────────────────────────────
    private bool _showStatus       = true;
    private bool _showUsername     = true;
    private bool _showEmail        = true;
    private bool _showPhone        = true;
    private bool _showNitro        = true;
    private bool _showNitroSince   = true;
    private bool _showTwoFA        = true;
    private bool _showLocale       = true;
    private bool _showVerified     = true;
    private bool _showConnected    = true;
    private bool _showPayment      = true;
    private bool _showToken        = true;
    private bool _showCreated      = true;  // nowa kolumna — data założenia konta

    public bool ShowStatus     { get => _showStatus;     set => Set(ref _showStatus,     value); }
    public bool ShowUsername   { get => _showUsername;   set => Set(ref _showUsername,   value); }
    public bool ShowEmail      { get => _showEmail;      set => Set(ref _showEmail,      value); }
    public bool ShowPhone      { get => _showPhone;      set => Set(ref _showPhone,      value); }
    public bool ShowNitro      { get => _showNitro;      set => Set(ref _showNitro,      value); }
    public bool ShowNitroSince { get => _showNitroSince; set => Set(ref _showNitroSince, value); }
    public bool ShowTwoFA      { get => _showTwoFA;      set => Set(ref _showTwoFA,      value); }
    public bool ShowLocale     { get => _showLocale;     set => Set(ref _showLocale,     value); }
    public bool ShowVerified   { get => _showVerified;   set => Set(ref _showVerified,   value); }
    public bool ShowConnected  { get => _showConnected;  set => Set(ref _showConnected,  value); }
    public bool ShowPayment    { get => _showPayment;    set => Set(ref _showPayment,    value); }
    public bool ShowToken      { get => _showToken;      set => Set(ref _showToken,      value); }
    public bool ShowCreated    { get => _showCreated;    set => Set(ref _showCreated,    value); }
}
