using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiscordTokenChecker.Models;

public enum CheckStatus { Valid, Invalid, Error, Checking }

public class TokenResult : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public CheckStatus Status          { get; set; }
    public string Token                { get; set; } = "";
    public string TokenShort           => Token.Length > 24 ? Token[..24] + "..." : Token;

    // Id — używane przez DiscordChecker i NotificationService
    public string Id                   { get; set; } = "";

    // UserId — alias dla Id, używany przez FetchAndApplyCreatedDateAsync w MainWindow
    public string UserId
    {
        get => Id;
        set => Id = value;
    }

    public string Username             { get; set; } = "";
    public string Email                { get; set; } = "";
    public string Phone                { get; set; } = "";
    public string NitroType            { get; set; } = "";
    public string NitroSince           { get; set; } = "";
    public string NitroBalance         { get; set; } = "";
    public string TwoFA                { get; set; } = "";
    public string Locale               { get; set; } = "";

    // bool — DiscordChecker przypisuje wynik GetBoolean()
    public bool Verified               { get; set; }

    public string ConnectedAccounts    { get; set; } = "";
    public string PaymentInfo          { get; set; } = "";

    // Data założenia konta — wypełniana asynchronicznie przez NicheProwler API
    private string _createdAt = "";
    public string CreatedAt
    {
        get => _createdAt;
        set { _createdAt = value; OnPropertyChanged(); }
    }
}
