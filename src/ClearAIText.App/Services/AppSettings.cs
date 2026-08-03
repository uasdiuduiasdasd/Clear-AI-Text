using ClearAIText.Core.Models;

namespace ClearAIText.App.Services;

/// <summary>
/// Persisted configuration state for the application.
/// </summary>
public sealed class AppSettings
{
    public NormalizationProfile Profile { get; set; } = NormalizationProfile.CreateSafeDefault();
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public string ThemeMode { get; set; } = "System";
    public string Language { get; set; } = LocalizationService.DetectSystemLanguage();
    public List<string> ExcludedProcesses { get; set; } =
    [
        "KeePass",
        "1Password",
        "Bitwarden",
        "Enpass",
        "NordPass",
        "LastPass"
    ];
}
