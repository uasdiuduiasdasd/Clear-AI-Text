using ClearAIText.App.Services;
using ClearAIText.Core.Models;
using ClearAIText.Core.Pipeline;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace ClearAIText.App.Views;

public sealed partial class SandboxPage : Page
{
    private readonly ITextPipeline _pipeline;
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _localizationService;
    private readonly DispatcherTimer _debounceTimer;
    private readonly DispatcherTimer _copyFeedbackTimer;
    private bool _isInitialized;

    public SandboxPage()
    {
        var app = (App)Application.Current;
        _pipeline = app.Pipeline;
        _settingsService = app.SettingsService;
        _localizationService = app.LocalizationService;

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _debounceTimer.Tick += (s, e) =>
        {
            _debounceTimer.Stop();
            ProcessCurrentText();
        };

        _copyFeedbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        _copyFeedbackTimer.Tick += (s, e) =>
        {
            _copyFeedbackTimer.Stop();
            if (CopyResultIcon != null && CopyResultButtonText != null)
            {
                CopyResultIcon.Glyph = "\uE8C8";
                CopyResultButtonText.Text = _localizationService["ButtonCopyResult"];
            }
        };

        InitializeComponent();

        ApplyLocalization();
        _localizationService.LanguageChanged += () => DispatcherQueue.TryEnqueue(ApplyLocalization);

        _isInitialized = true;
        Loaded += SandboxPage_Loaded;
    }

    private void SandboxPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateMonitoringStatusHint();
        ProcessCurrentText();
    }

    public void ApplyLocalization()
    {
        if (SandboxPageTitle == null)
        {
            return;
        }

        SandboxPageTitle.Text = _localizationService["SandboxTitle"];
        SandboxPageSubtitle.Text = _localizationService["SandboxSubtitle"];
        ModeLabel.Text = _localizationService["SandboxModeLabel"];

        ModeTier1Item.Content = _localizationService["SandboxModeTier1"];
        ModeAggressiveItem.Content = _localizationService["SandboxModeAggressive"];
        ModeCustomOnlyItem.Content = _localizationService["SandboxModeCustomOnly"];
        ModeCurrentItem.Content = _localizationService["SandboxModeCurrent"];

        PasteButtonText.Text = _localizationService["ButtonPaste"];
        SwapButtonText.Text = _localizationService["ButtonSwap"];
        ClearButtonText.Text = _localizationService["ButtonClear"];
        CopyResultButtonText.Text = _localizationService["ButtonCopyResult"];

        InputHeaderLabel.Text = _localizationService["InputHeader"];
        RawInputTextBox.PlaceholderText = _localizationService["InputPlaceholder"];

        OutputHeaderLabel.Text = _localizationService["OutputHeader"];
        CleanedOutputTextBox.PlaceholderText = _localizationService["OutputPlaceholder"];

        AnalysisResultsTitle.Text = _localizationService["AnalysisResultsTitle"];
        MonitoringHintInfoBar.Message = _localizationService["MonitoringDisabledHint"];

        UpdateMonitoringStatusHint();
        ProcessCurrentText();
    }

    private void UpdateMonitoringStatusHint()
    {
        if (MonitoringHintInfoBar == null || _settingsService?.Settings?.Profile == null)
        {
            return;
        }

        bool isGlobalDisabled = !_settingsService.Settings.Profile.IsEnabled;
        MonitoringHintInfoBar.IsOpen = isGlobalDisabled;
    }

    private void RawInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        _debounceTimer.Stop();
        if (string.IsNullOrEmpty(RawInputTextBox.Text))
        {
            ProcessCurrentText();
        }
        else
        {
            _debounceTimer.Start();
        }
    }

    private void SandboxModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        ProcessCurrentText();
    }

    private NormalizationProfile GetActiveSandboxProfile()
    {
        int selectedIndex = SandboxModeComboBox?.SelectedIndex ?? 0;
        var currentProfile = _settingsService.Settings.Profile;

        switch (selectedIndex)
        {
            case 0: // Tier 1: Safe Typography (Default)
            {
                var p = NormalizationProfile.CreateSafeDefault();
                p.IsEnabled = true;
                p.CustomRules = currentProfile.CustomRules;
                return p;
            }
            case 1: // Tier 1 + 2: Aggressive Cleaning
            {
                var p = NormalizationProfile.CreateAggressiveClean();
                p.IsEnabled = true;
                p.CustomRules = currentProfile.CustomRules;
                return p;
            }
            case 2: // Custom rules only
            {
                return new NormalizationProfile
                {
                    Name = "Custom Rules Only",
                    IsEnabled = true,
                    ReplaceDashes = false,
                    ReplaceQuotes = false,
                    NormalizeSpaces = false,
                    CleanInvisibleControls = false,
                    NormalizeEllipses = false,
                    StripDiacritics = false,
                    StripEmojis = false,
                    StripLightMarkdown = false,
                    DetectConfusables = false,
                    CustomRules = currentProfile.CustomRules
                };
            }
            default: // According to settings
            {
                return new NormalizationProfile
                {
                    Name = currentProfile.Name,
                    IsEnabled = true, // Force enabled in sandbox so user can test their configured rules
                    ReplaceDashes = currentProfile.ReplaceDashes,
                    ReplaceQuotes = currentProfile.ReplaceQuotes,
                    NormalizeSpaces = currentProfile.NormalizeSpaces,
                    CleanInvisibleControls = currentProfile.CleanInvisibleControls,
                    NormalizeEllipses = currentProfile.NormalizeEllipses,
                    StripDiacritics = currentProfile.StripDiacritics,
                    StripEmojis = currentProfile.StripEmojis,
                    StripLightMarkdown = currentProfile.StripLightMarkdown,
                    DetectConfusables = currentProfile.DetectConfusables,
                    CustomRules = currentProfile.CustomRules
                };
            }
        }
    }

    private void ProcessCurrentText()
    {
        if (RawInputTextBox == null || CleanedOutputTextBox == null || _localizationService == null || _pipeline == null)
        {
            return;
        }

        UpdateMonitoringStatusHint();

        string rawText = RawInputTextBox.Text;
        if (string.IsNullOrEmpty(rawText))
        {
            CleanedOutputTextBox.Text = string.Empty;
            InputStatsLabel.Text = _localizationService.GetString("StatInputLength", 0, 0);
            OutputStatsLabel.Text = _localizationService["StatOutputLengthZero"];
            TimingStatsLabel.Text = _localizationService.GetString("StatTimeFormat", 0.0);
            RuleSummaryText.Text = _localizationService["EnterTextHint"];
            HomoglyphWarningText.Visibility = Visibility.Collapsed;
            return;
        }

        int inputLines = rawText.Split(["\r\n", "\r", "\n", "\u2028", "\u2029", "\u0085"], StringSplitOptions.None).Length;
        InputStatsLabel.Text = _localizationService.GetString("StatInputLength", rawText.Length, inputLines);

        var profile = GetActiveSandboxProfile();
        var result = _pipeline.Process(rawText, profile);
        CleanedOutputTextBox.Text = result.OutputText;

        int delta = result.OutputText.Length - rawText.Length;
        OutputStatsLabel.Text = _localizationService.GetString("StatOutputLength", result.OutputText.Length, delta);
        TimingStatsLabel.Text = _localizationService.GetString("StatTimeFormat", result.ElapsedTime.TotalMilliseconds);

        if (result.TotalReplacementsCount == 0)
        {
            RuleSummaryText.Text = _localizationService["NoModificationsNeeded"];
        }
        else
        {
            var summary = result.AppliedRules
                .Where(r => r.ReplacementsCount > 0)
                .Select(r => $"{r.RuleName}: {r.ReplacementsCount}");

            string labelReplacements = _localizationService.GetString("StatReplacementsFormat", result.TotalReplacementsCount);
            string labelTime = _localizationService.GetString("StatTimeFormat", result.ElapsedTime.TotalMilliseconds);

            RuleSummaryText.Text = $"{labelReplacements} | {labelTime} | {string.Join(", ", summary)}";
        }

        // Check for homoglyph warnings (Tier 3 Heuristic)
        if (result.ConfusablesDetected.Count > 0)
        {
            string confusableWords = string.Join(", ", result.ConfusablesDetected.Take(4));
            HomoglyphWarningText.Text = _localizationService.GetString("HomoglyphsFoundFormat", result.ConfusablesDetected.Count, confusableWords);
            HomoglyphWarningText.Visibility = Visibility.Visible;
        }
        else
        {
            HomoglyphWarningText.Visibility = Visibility.Collapsed;
        }
    }

    private async void PasteFromClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                RawInputTextBox.Text = text;
                ProcessCurrentText();
            }
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            // Clipboard may be temporarily locked or inaccessible
        }
    }

    private void SwapInputOutputButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(CleanedOutputTextBox.Text))
        {
            RawInputTextBox.Text = CleanedOutputTextBox.Text;
            ProcessCurrentText();
        }
    }

    private void ClearInputsButton_Click(object sender, RoutedEventArgs e)
    {
        RawInputTextBox.Text = string.Empty;
        CleanedOutputTextBox.Text = string.Empty;
        ProcessCurrentText();
    }

    private void CopyResultButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string text = CleanedOutputTextBox.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(text);
                Clipboard.SetContent(dataPackage);

                CopyResultIcon.Glyph = "\uE73E"; // Checkmark icon
                CopyResultButtonText.Text = _localizationService["ButtonCopied"];
                _copyFeedbackTimer.Stop();
                _copyFeedbackTimer.Start();
            }
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            // Clipboard may be temporarily locked
        }
    }
}
