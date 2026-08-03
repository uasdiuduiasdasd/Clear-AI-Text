using ClearAIText.App.Services;
using ClearAIText.Core.Pipeline;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Linq;

namespace ClearAIText.App;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "WinUI 3 Application lifecycle manages lifetime until process exit.")]
public partial class App : Application
{
    private MainWindow? _mainWindow;
    public MainWindow? MainWindow => _mainWindow;

    public ITextPipeline Pipeline { get; }
    public SettingsService SettingsService { get; }
    public LocalizationService LocalizationService { get; }
    public ClipboardService ClipboardService { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        Pipeline = new TextPipeline();
        SettingsService = new SettingsService();
        LocalizationService = new LocalizationService
        {
            CurrentLanguage = SettingsService.Settings.Language
        };

        UnhandledException += (s, e) =>
        {
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        ClipboardService = new ClipboardService(Pipeline, SettingsService, LocalizationService, dispatcherQueue);
        ClipboardService.Start();

        _mainWindow = new MainWindow();

        string[] cmdArgs = Environment.GetCommandLineArgs();
        bool startMinimized = cmdArgs.Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

        if (!startMinimized)
        {
            _mainWindow.Activate();
        }
    }
}
