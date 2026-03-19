using System.Windows;
using DiscordTokenChecker.Models;

namespace DiscordTokenChecker;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings = AppSettings.Instance;

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = _settings;
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowStatus     = true;
        _settings.ShowUsername   = true;
        _settings.ShowEmail      = true;
        _settings.ShowPhone      = true;
        _settings.ShowNitro      = true;
        _settings.ShowNitroSince = true;
        _settings.ShowTwoFA      = true;
        _settings.ShowLocale     = true;
        _settings.ShowVerified   = true;
        _settings.ShowConnected  = true;
        _settings.ShowPayment    = true;
        _settings.ShowToken      = true;
        _settings.ShowCreated    = true;
    }

    private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowStatus     = false;
        _settings.ShowUsername   = false;
        _settings.ShowEmail      = false;
        _settings.ShowPhone      = false;
        _settings.ShowNitro      = false;
        _settings.ShowNitroSince = false;
        _settings.ShowTwoFA      = false;
        _settings.ShowLocale     = false;
        _settings.ShowVerified   = false;
        _settings.ShowConnected  = false;
        _settings.ShowPayment    = false;
        _settings.ShowToken      = false;
        _settings.ShowCreated    = false;
    }
}
