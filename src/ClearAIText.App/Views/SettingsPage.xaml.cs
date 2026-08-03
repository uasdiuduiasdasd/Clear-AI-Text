using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClearAIText.App.Services;
using ClearAIText.Core.Models;
using ClearAIText.Windows.Process;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ClearAIText.App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _localizationService;
    private readonly ObservableCollection<string> _excludedProcesses = [];
    private readonly ObservableCollection<CustomRuleItemViewModel> _customRules = [];
    private CustomRuleItemViewModel? _editingRuleViewModel;
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();

        var app = (App)Application.Current;
        _settingsService = app.SettingsService;
        _localizationService = app.LocalizationService;

        LoadSettingsToUI();
        ApplyLocalization();

        _localizationService.LanguageChanged += () => DispatcherQueue.TryEnqueue(ApplyLocalization);
        _isInitializing = false;
    }

    private void LoadSettingsToUI()
    {
        var profile = _settingsService.Settings.Profile;

        // Theme combo
        string theme = _settingsService.Settings.ThemeMode ?? "System";
        ThemeModeComboBox.SelectedIndex = theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        // Language combo
        string lang = _settingsService.Settings.Language ?? "ru";
        LanguageComboBox.SelectedIndex = lang.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

        MasterNormalizationToggle.IsOn = profile.IsEnabled;

        NormalizeDashesToggle.IsOn = profile.ReplaceDashes;
        NormalizeQuotesToggle.IsOn = profile.ReplaceQuotes;
        NormalizeWhitespacesToggle.IsOn = profile.NormalizeSpaces;
        CleanInvisibleToggle.IsOn = profile.CleanInvisibleControls;

        StripDiacriticsToggle.IsOn = profile.StripDiacritics;
        StripEmojisToggle.IsOn = profile.StripEmojis;
        StripMarkdownToggle.IsOn = profile.StripLightMarkdown;

        MinimizeToTrayToggle.IsOn = _settingsService.Settings.MinimizeToTrayOnClose;
        StartWithWindowsToggle.IsOn = StartupService.IsRunOnStartupEnabled();

        _customRules.Clear();
        foreach (var rule in profile.CustomRules)
        {
            _customRules.Add(new CustomRuleItemViewModel(rule, _localizationService));
        }
        CustomRulesListView.ItemsSource = _customRules;
        UpdateEmptyCustomRulesVisibility();

        _excludedProcesses.Clear();
        foreach (string proc in _settingsService.Settings.ExcludedProcesses)
        {
            _excludedProcesses.Add(proc);
        }

        ExcludedProcessesListView.ItemsSource = _excludedProcesses;
        UpdateEmptyProcessesVisibility();
    }

    public void ApplyLocalization()
    {
        SettingsPageTitle.Text = _localizationService["SettingsTitle"];
        SettingsPageSubtitle.Text = _localizationService["SettingsSubtitle"];

        // Appearance
        AppearanceGroupTitle.Text = _localizationService["GroupAppearance"];
        AppearanceGroupDesc.Text = _localizationService["GroupAppearanceDesc"];
        ThemeHeaderLabel.Text = _localizationService["ThemeHeader"];
        if (ThemeModeComboBox.Items.Count >= 3)
        {
            ((ComboBoxItem)ThemeModeComboBox.Items[0]).Content = _localizationService["ThemeSystem"];
            ((ComboBoxItem)ThemeModeComboBox.Items[1]).Content = _localizationService["ThemeLight"];
            ((ComboBoxItem)ThemeModeComboBox.Items[2]).Content = _localizationService["ThemeDark"];
        }

        LanguageHeaderLabel.Text = _localizationService["LanguageHeader"];
        if (LanguageComboBox.Items.Count >= 2)
        {
            ((ComboBoxItem)LanguageComboBox.Items[0]).Content = _localizationService["LanguageRussian"];
            ((ComboBoxItem)LanguageComboBox.Items[1]).Content = _localizationService["LanguageEnglish"];
        }

        // Master
        MasterToggleTitle.Text = _localizationService["ToggleMaster"];
        MasterToggleDesc.Text = _localizationService["ToggleMasterDesc"];
        MasterNormalizationToggle.OnContent = _localizationService["ToggleOn"];
        MasterNormalizationToggle.OffContent = _localizationService["ToggleOff"];

        // Tier 1
        Tier1GroupTitle.Text = _localizationService["GroupTier1"];
        Tier1GroupDesc.Text = _localizationService["GroupTier1Desc"];
        NormalizeDashesToggle.Header = _localizationService["ToggleDashes"];
        NormalizeDashesToggle.OnContent = _localizationService["ToggleEnabled"];
        NormalizeDashesToggle.OffContent = _localizationService["ToggleDisabled"];

        NormalizeQuotesToggle.Header = _localizationService["ToggleQuotes"];
        NormalizeQuotesToggle.OnContent = _localizationService["ToggleEnabled"];
        NormalizeQuotesToggle.OffContent = _localizationService["ToggleDisabled"];

        NormalizeWhitespacesToggle.Header = _localizationService["ToggleSpaces"];
        NormalizeWhitespacesToggle.OnContent = _localizationService["ToggleEnabled"];
        NormalizeWhitespacesToggle.OffContent = _localizationService["ToggleDisabled"];

        CleanInvisibleToggle.Header = _localizationService["ToggleInvisible"];
        CleanInvisibleToggle.OnContent = _localizationService["ToggleEnabled"];
        CleanInvisibleToggle.OffContent = _localizationService["ToggleDisabled"];

        // Custom Rules
        CustomRulesGroupTitle.Text = _localizationService["GroupCustomRules"];
        CustomRulesGroupDesc.Text = _localizationService["GroupCustomRulesDesc"];
        RulePatternTextBox.Header = _localizationService["RulePatternHeader"];
        RulePatternTextBox.PlaceholderText = _localizationService["RulePatternPlaceholder"];
        RuleActionComboBox.Header = _localizationService["RuleActionHeader"];
        if (RuleActionComboBox.Items.Count >= 2)
        {
            ((ComboBoxItem)RuleActionComboBox.Items[0]).Content = _localizationService["RuleActionDelete"];
            ((ComboBoxItem)RuleActionComboBox.Items[1]).Content = _localizationService["RuleActionReplace"];
        }

        RuleReplacementTextBox.Header = _localizationService["RuleReplacementHeader"];
        RuleIsRegexCheckBox.Content = _localizationService["RuleIsRegex"];
        RuleRegexHintText.Text = _localizationService["RuleRegexHint"];
        AddRuleButton.Content = _editingRuleViewModel != null ? _localizationService["ButtonSaveRule"] : _localizationService["ButtonAddRule"];
        CancelEditButton.Content = _localizationService["ButtonCancelRule"];
        CustomRulesListHeader.Text = _localizationService["CustomRulesListHeader"];
        EmptyCustomRulesText.Text = _localizationService["EmptyCustomRulesText"];

        // Tier 2
        Tier2GroupTitle.Text = _localizationService["GroupTier2"];
        Tier2GroupDesc.Text = _localizationService["GroupTier2Desc"];
        StripDiacriticsToggle.Header = _localizationService["ToggleDiacritics"];
        StripDiacriticsToggle.OnContent = _localizationService["ToggleEnabled"];
        StripDiacriticsToggle.OffContent = _localizationService["ToggleDisabled"];

        StripEmojisToggle.Header = _localizationService["ToggleEmojis"];
        StripEmojisToggle.OnContent = _localizationService["ToggleEnabled"];
        StripEmojisToggle.OffContent = _localizationService["ToggleDisabled"];

        StripMarkdownToggle.Header = _localizationService["ToggleMarkdown"];
        StripMarkdownToggle.OnContent = _localizationService["ToggleEnabled"];
        StripMarkdownToggle.OffContent = _localizationService["ToggleDisabled"];

        // Exclusions
        ExclusionsGroupTitle.Text = _localizationService["GroupExclusions"];
        ExclusionsGroupDesc.Text = _localizationService["GroupExclusionsDesc"];
        NewProcessTextBox.PlaceholderText = _localizationService["ProcessPlaceholder"];
        AddProcessButton.Content = _localizationService["ButtonAddProcess"];
        PickRunningAppButton.Content = _localizationService["ButtonPickRunning"];
        EmptyExcludedProcessesText.Text = _localizationService["EmptyExcludedProcessesText"];

        // System
        SystemGroupTitle.Text = _localizationService["GroupSystem"];
        StartWithWindowsToggle.Header = _localizationService["ToggleStartWithWindows"];
        StartWithWindowsToggle.OnContent = _localizationService["ToggleOn"];
        StartWithWindowsToggle.OffContent = _localizationService["ToggleOff"];

        MinimizeToTrayToggle.Header = _localizationService["ToggleMinimizeToTray"];
        MinimizeToTrayToggle.OnContent = _localizationService["ToggleMinimizeToTrayOn"];
        MinimizeToTrayToggle.OffContent = _localizationService["ToggleMinimizeToTrayOff"];

        foreach (var ruleVm in _customRules)
        {
            ruleVm.RefreshLocalization();
        }
    }

    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        string theme = ThemeModeComboBox.SelectedIndex switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };

        _settingsService.Settings.ThemeMode = theme;
        _settingsService.SaveSettings();

        // Apply theme to main window
        var app = (App)Application.Current;
        app.MainWindow?.ApplyTheme(theme);
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        string lang = LanguageComboBox.SelectedIndex == 0 ? "ru" : "en";
        _settingsService.Settings.Language = lang;
        _settingsService.SaveSettings();

        _localizationService.CurrentLanguage = lang;
    }

    private void UpdateEmptyCustomRulesVisibility()
    {
        EmptyCustomRulesText.Visibility = _customRules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateEmptyProcessesVisibility()
    {
        EmptyExcludedProcessesText.Visibility = _excludedProcesses.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SettingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        var profile = _settingsService.Settings.Profile;
        profile.IsEnabled = MasterNormalizationToggle.IsOn;
        profile.ReplaceDashes = NormalizeDashesToggle.IsOn;
        profile.ReplaceQuotes = NormalizeQuotesToggle.IsOn;
        profile.NormalizeSpaces = NormalizeWhitespacesToggle.IsOn;
        profile.CleanInvisibleControls = CleanInvisibleToggle.IsOn;

        profile.StripDiacritics = StripDiacriticsToggle.IsOn;
        profile.StripEmojis = StripEmojisToggle.IsOn;
        profile.StripLightMarkdown = StripMarkdownToggle.IsOn;

        _settingsService.SaveSettings();
    }

    private void RuleActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RuleReplacementTextBox == null)
        {
            return;
        }

        bool isReplace = RuleActionComboBox.SelectedIndex == 1;
        RuleReplacementTextBox.IsEnabled = isReplace;
    }

    private static void ShowFlyout(FrameworkElement target, string message)
    {
        var flyout = new Flyout
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom,
            Content = new TextBlock
            {
                Text = message,
                MaxWidth = 260,
                TextWrapping = TextWrapping.Wrap
            }
        };
        flyout.ShowAt(target);
    }

    private void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        string pattern = RulePatternTextBox.Text.Trim();
        if (string.IsNullOrEmpty(pattern))
        {
            ShowFlyout(AddRuleButton, _localizationService["ValidationEmptyRule"]);
            RulePatternTextBox.Focus(FocusState.Programmatic);
            return;
        }

        bool isRegex = RuleIsRegexCheckBox.IsChecked == true;
        if (isRegex)
        {
            try
            {
                _ = new System.Text.RegularExpressions.Regex(pattern);
            }
            catch (ArgumentException)
            {
                ShowFlyout(AddRuleButton, _localizationService["ValidationInvalidRegex"]);
                RulePatternTextBox.Focus(FocusState.Programmatic);
                return;
            }
        }

        bool isReplace = RuleActionComboBox.SelectedIndex == 1;
        string replacement = isReplace ? RuleReplacementTextBox.Text : string.Empty;

        if (_editingRuleViewModel != null)
        {
            // Update existing rule
            var updatedRule = _editingRuleViewModel.Rule with
            {
                Name = isReplace ? $"Замена: {pattern} -> {replacement}" : $"Удаление: {pattern}",
                FindPattern = pattern,
                Replacement = replacement,
                IsRegex = isRegex
            };

            _editingRuleViewModel.Rule = updatedRule;
            _editingRuleViewModel = null;
            AddRuleButton.Content = _localizationService["ButtonAddRule"];
            CancelEditButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Add new rule
            var newRule = new CustomRule
            {
                Name = isReplace ? $"Замена: {pattern} -> {replacement}" : $"Удаление: {pattern}",
                FindPattern = pattern,
                Replacement = replacement,
                IsRegex = isRegex,
                IsEnabled = true
            };

            var vm = new CustomRuleItemViewModel(newRule, _localizationService);
            _customRules.Add(vm);
        }

        SyncCustomRulesToProfile();
        UpdateEmptyCustomRulesVisibility();

        ResetRuleForm();
    }

    private void EditRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CustomRuleItemViewModel vm })
        {
            _editingRuleViewModel = vm;
            RulePatternTextBox.Text = vm.Rule.FindPattern;
            bool isReplace = !string.IsNullOrEmpty(vm.Rule.Replacement);
            RuleActionComboBox.SelectedIndex = isReplace ? 1 : 0;
            RuleReplacementTextBox.Text = vm.Rule.Replacement ?? string.Empty;
            RuleReplacementTextBox.IsEnabled = isReplace;
            RuleIsRegexCheckBox.IsChecked = vm.Rule.IsRegex;

            AddRuleButton.Content = _localizationService["ButtonSaveRule"];
            CancelEditButton.Visibility = Visibility.Visible;
            RulePatternTextBox.Focus(FocusState.Programmatic);
        }
    }

    private void CancelEditButton_Click(object sender, RoutedEventArgs e)
    {
        _editingRuleViewModel = null;
        ResetRuleForm();
    }

    private void ResetRuleForm()
    {
        RulePatternTextBox.Text = string.Empty;
        RuleReplacementTextBox.Text = string.Empty;
        RuleIsRegexCheckBox.IsChecked = false;
        RuleActionComboBox.SelectedIndex = 0;
        RuleReplacementTextBox.IsEnabled = false;
        AddRuleButton.Content = _localizationService["ButtonAddRule"];
        CancelEditButton.Visibility = Visibility.Collapsed;
    }

    private void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CustomRuleItemViewModel vm })
        {
            if (_editingRuleViewModel == vm)
            {
                _editingRuleViewModel = null;
                ResetRuleForm();
            }

            _ = _customRules.Remove(vm);
            SyncCustomRulesToProfile();
            UpdateEmptyCustomRulesVisibility();
        }
    }

    private void RuleToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (sender is ToggleSwitch toggle && toggle.Tag is CustomRuleItemViewModel vm)
        {
            vm.IsEnabled = toggle.IsOn;
            vm.Rule = vm.Rule with { IsEnabled = toggle.IsOn };
            SyncCustomRulesToProfile();
        }
    }

    private void SyncCustomRulesToProfile()
    {
        var profile = _settingsService.Settings.Profile;
        profile.CustomRules = _customRules.Select(vm => vm.Rule with { IsEnabled = vm.IsEnabled }).ToList();
        _settingsService.SaveSettings();
    }

    private void StartWithWindowsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        StartupService.SetRunOnStartup(StartWithWindowsToggle.IsOn);
        _settingsService.Settings.StartWithWindows = StartWithWindowsToggle.IsOn;
        _settingsService.SaveSettings();
    }

    private void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settingsService.Settings.MinimizeToTrayOnClose = MinimizeToTrayToggle.IsOn;
        _settingsService.SaveSettings();
    }

    private void NewProcessTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == global::Windows.System.VirtualKey.Enter)
        {
            AddProcess();
            e.Handled = true;
        }
    }

    private void AddProcessButton_Click(object sender, RoutedEventArgs e)
    {
        AddProcess();
    }

    private void AddProcess()
    {
        string processName = NewProcessTextBox.Text.Trim();
        if (string.IsNullOrEmpty(processName))
        {
            ShowFlyout(AddProcessButton, _localizationService["ValidationEmptyProcess"]);
            NewProcessTextBox.Focus(FocusState.Programmatic);
            return;
        }

        if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            processName = processName[..^4].Trim();
        }

        if (string.IsNullOrEmpty(processName))
        {
            ShowFlyout(AddProcessButton, _localizationService["ValidationInvalidProcess"]);
            return;
        }

        if (_excludedProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
        {
            ShowFlyout(AddProcessButton, _localizationService.GetString("ValidationDuplicateProcess", processName));
            return;
        }

        _excludedProcesses.Add(processName);
        _settingsService.Settings.ExcludedProcesses = _excludedProcesses.ToList();
        _settingsService.SaveSettings();
        UpdateEmptyProcessesVisibility();

        NewProcessTextBox.Text = string.Empty;
    }

    private void PickRunningAppButton_Click(object sender, RoutedEventArgs e)
    {
        var runningApps = RunningAppsDetector.GetRunningWindowsApps();
        var unexcludedApps = runningApps
            .Where(a => !_excludedProcesses.Contains(a.ProcessName, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var stackPanel = new StackPanel { Width = 320, MaxHeight = 360, Spacing = 8 };
        stackPanel.Children.Add(new TextBlock
        {
            Text = _localizationService["RunningAppsHeader"],
            Style = (Style)Application.Current.Resources["BaseTextBlockStyle"],
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = _localizationService["RunningAppsDesc"],
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap
        });

        var flyout = new Flyout
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
        };

        if (unexcludedApps.Count == 0)
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = _localizationService["EmptyRunningAppsText"],
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
        }
        else
        {
            var listView = new ListView { SelectionMode = ListViewSelectionMode.None, MaxHeight = 240 };
            foreach (var appInfo in unexcludedApps)
            {
                var btn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 1, 0, 1),
                    Tag = appInfo.ProcessName
                };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var sp = new StackPanel { Spacing = 2 };
                sp.Children.Add(new TextBlock { Text = appInfo.WindowTitle, TextTrimming = TextTrimming.CharacterEllipsis, FontWeight = Microsoft.UI.Text.FontWeights.Medium });
                sp.Children.Add(new TextBlock { Text = appInfo.ExeName, Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"], FontFamily = new FontFamily("Consolas, monospace") });
                Grid.SetColumn(sp, 0);
                grid.Children.Add(sp);

                var plus = new TextBlock { Text = "+", VerticalAlignment = VerticalAlignment.Center, FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 16, Margin = new Thickness(8, 0, 0, 0), Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"] };
                Grid.SetColumn(plus, 1);
                grid.Children.Add(plus);

                btn.Content = grid;
                btn.Click += (s, args) =>
                {
                    if (btn.Tag is string pName && !string.IsNullOrWhiteSpace(pName))
                    {
                        if (!_excludedProcesses.Contains(pName, StringComparer.OrdinalIgnoreCase))
                        {
                            _excludedProcesses.Add(pName);
                            _settingsService.Settings.ExcludedProcesses = _excludedProcesses.ToList();
                            _settingsService.SaveSettings();
                            UpdateEmptyProcessesVisibility();
                        }
                        flyout.Hide();
                    }
                };
                listView.Items.Add(btn);
            }
            stackPanel.Children.Add(listView);
        }

        flyout.Content = stackPanel;
        flyout.ShowAt(PickRunningAppButton);
    }

    private void RemoveProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string processName })
        {
            _ = _excludedProcesses.Remove(processName);
            _settingsService.Settings.ExcludedProcesses = _excludedProcesses.ToList();
            _settingsService.SaveSettings();
            UpdateEmptyProcessesVisibility();
        }
    }
}

public sealed class CustomRuleItemViewModel : INotifyPropertyChanged
{
    private CustomRule _rule;
    private bool _isEnabled;
    private readonly LocalizationService _localizationService;

    public CustomRule Rule
    {
        get => _rule;
        set
        {
            _rule = value;
            RefreshLocalization();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public string DisplayPattern => _rule.FindPattern;

    public string DisplayAction => string.IsNullOrEmpty(_rule.Replacement)
        ? _localizationService["ActionDeleteBadge"]
        : $"→ {_rule.Replacement}";

    public Brush BadgeBackgroundBrush => string.IsNullOrEmpty(_rule.Replacement)
        ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 230, 80, 80))
        : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 0, 180, 160));

    public Brush BadgeForegroundBrush => string.IsNullOrEmpty(_rule.Replacement)
        ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 120, 120))
        : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 240, 220));

    public Visibility RegexVisibility => _rule.IsRegex ? Visibility.Visible : Visibility.Collapsed;

    public CustomRuleItemViewModel(CustomRule rule, LocalizationService localizationService)
    {
        _rule = rule;
        _isEnabled = rule.IsEnabled;
        _localizationService = localizationService;
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(DisplayPattern));
        OnPropertyChanged(nameof(DisplayAction));
        OnPropertyChanged(nameof(BadgeBackgroundBrush));
        OnPropertyChanged(nameof(BadgeForegroundBrush));
        OnPropertyChanged(nameof(RegexVisibility));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
