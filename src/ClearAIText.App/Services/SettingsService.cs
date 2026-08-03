using System.IO;
using System.Security;
using System.Text.Json;
using ClearAIText.Core.Models;

namespace ClearAIText.App.Services;

/// <summary>
/// Service managing local configuration loading and persistence.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsFilePath;
    private readonly AppSettings _currentSettings;

    public AppSettings Settings => _currentSettings;

    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(localAppData, "ClearAIText");
        _ = Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");

        _currentSettings = LoadSettings();
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonSerializer.Serialize(_currentSettings, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or SecurityException)
        {
            // Fallback gracefully on I/O error
        }
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    if (string.IsNullOrWhiteSpace(loaded.Language))
                    {
                        loaded.Language = LocalizationService.DetectSystemLanguage();
                    }
                    return loaded;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or SecurityException)
        {
            // Return defaults if corrupted
        }

        return new AppSettings();
    }
}
