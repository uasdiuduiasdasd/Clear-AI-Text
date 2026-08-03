using ClearAIText.App.Services;
using ClearAIText.App.Views;
using ClearAIText.Windows.Theme;
using ClearAIText.Windows.Tray;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using Windows.Graphics;
using WinRT.Interop;

namespace ClearAIText.App;

public sealed partial class MainWindow : Window, IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _localizationService;
    private readonly TrayIconManager _trayIconManager;
    private readonly nint _hwnd;
    private bool _isExplicitExit;
    private bool _disposed;

    public MainWindow()
    {
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);

        var app = (App)Application.Current;
        _settingsService = app.SettingsService;
        _localizationService = app.LocalizationService;

        SetupWindowChromeAndDimensions();

        _trayIconManager = new TrayIconManager(
            onRestoreRequested: RestoreAndFocusWindow,
            onExitRequested: ForceExitApplication,
            onNavigateSandboxRequested: NavigateToSandbox,
            onNavigateSettingsRequested: NavigateToSettings,
            isMonitoringActiveGetter: () => app.ClipboardService?.IsMonitoringEnabled ?? true,
            onToggleMonitoringRequested: (active) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (app.ClipboardService != null)
                    {
                        app.ClipboardService.IsMonitoringEnabled = active;
                    }
                });
            },
            stringGetter: key => _localizationService.GetString(key));

        AppWindow.Closing += AppWindow_Closing;

        if (Content is FrameworkElement rootElement)
        {
            rootElement.ActualThemeChanged += (s, e) => UpdateTitleBarAndTheme();
        }

        ApplyTheme(_settingsService.Settings.ThemeMode);
        ApplyLocalization();

        _localizationService.LanguageChanged += () => DispatcherQueue.TryEnqueue(ApplyLocalization);

        NavView.SelectedItem = NavView.MenuItems[0];
    }

    public void ApplyTheme(string themeMode)
    {
        ElementTheme elementTheme = themeMode switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = elementTheme;
        }

        UpdateTitleBarAndTheme();
    }

    public void ApplyLocalization()
    {
        NavMonitorItem.Content = _localizationService["NavMonitor"];
        NavSandboxItem.Content = _localizationService["NavSandbox"];
        NavSettingsItem.Content = _localizationService["NavSettings"];
        NavAboutItem.Content = _localizationService["NavAbout"];
        NavExitItem.Content = _localizationService["NavExit"];
    }

    private void SetupWindowChromeAndDimensions()
    {
        Title = "Clear AI Text";

        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        AppWindow.Resize(new SizeInt32(960, 700));

        // Center window on screen if display area is available
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea != null)
        {
            var centeredPosition = new PointInt32(
                (displayArea.WorkArea.Width - 960) / 2,
                (displayArea.WorkArea.Height - 700) / 2);
            AppWindow.Move(centeredPosition);
        }

        UpdateTitleBarAndTheme();
    }

    private void UpdateTitleBarAndTheme()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        bool isDark = (Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ||
                      ((Content as FrameworkElement)?.ActualTheme == ElementTheme.Default &&
                       Application.Current.RequestedTheme == ApplicationTheme.Dark);

        WindowThemeHelper.SetImmersiveDarkMode(_hwnd, isDark);

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;

            titleBar.ButtonBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonInactiveBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);

            if (isDark)
            {
                titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                titleBar.ButtonHoverBackgroundColor = global::Windows.UI.Color.FromArgb(35, 255, 255, 255);
                titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                titleBar.ButtonPressedBackgroundColor = global::Windows.UI.Color.FromArgb(60, 255, 255, 255);
                titleBar.ButtonInactiveForegroundColor = global::Windows.UI.Color.FromArgb(120, 255, 255, 255);
            }
            else
            {
                titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                titleBar.ButtonHoverBackgroundColor = global::Windows.UI.Color.FromArgb(25, 0, 0, 0);
                titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
                titleBar.ButtonPressedBackgroundColor = global::Windows.UI.Color.FromArgb(45, 0, 0, 0);
                titleBar.ButtonInactiveForegroundColor = global::Windows.UI.Color.FromArgb(120, 0, 0, 0);
            }
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            NavigateByTag(item.Tag?.ToString());
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem containerItem)
        {
            NavigateByTag(containerItem.Tag?.ToString());
        }
        else if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            NavigateByTag(selectedItem.Tag?.ToString());
        }
    }

    private void NavigateByTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return;
        }

        if (tag == "Exit")
        {
            ForceExitApplication();
            return;
        }

        Type pageType = tag switch
        {
            "Monitor" => typeof(MonitorPage),
            "Sandbox" => typeof(SandboxPage),
            "Settings" => typeof(SettingsPage),
            "About" => typeof(AboutPage),
            _ => typeof(MonitorPage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            _ = ContentFrame.Navigate(pageType);
        }
    }

    public void NavigateToSandbox()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RestoreAndFocusWindow();
            foreach (var item in NavView.MenuItems)
            {
                if (item is NavigationViewItem navItem && (string?)navItem.Tag == "Sandbox")
                {
                    NavView.SelectedItem = navItem;
                    break;
                }
            }
        });
    }

    public void NavigateToSettings()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RestoreAndFocusWindow();
            foreach (var item in NavView.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem && (string?)navItem.Tag == "Settings")
                {
                    NavView.SelectedItem = navItem;
                    break;
                }
            }
        });
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!_isExplicitExit && _settingsService.Settings.MinimizeToTrayOnClose)
        {
            args.Cancel = true;
            AppWindow.Hide();
        }
        else
        {
            _trayIconManager.Dispose();
            var app = (App)Application.Current;
            app.ClipboardService?.Dispose();
        }
    }

    public void RestoreAndFocusWindow()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            AppWindow.Show();
            Activate();
        });
    }

    public void ForceExitApplication()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _isExplicitExit = true;
            _trayIconManager.Dispose();
            var app = (App)Application.Current;
            app.ClipboardService?.Dispose();
            Application.Current.Exit();
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _trayIconManager.Dispose();
            var app = (App)Application.Current;
            app.ClipboardService?.Dispose();
            _disposed = true;
        }
    }
}
