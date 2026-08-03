using System.Collections.ObjectModel;
using System.Globalization;
using ClearAIText.App.Models;
using ClearAIText.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace ClearAIText.App.Views;

public sealed partial class MonitorPage : Page
{
    private readonly ClipboardService _clipboardService;
    private readonly LocalizationService _localizationService;
    private bool _isSubscribed;

    public ObservableCollection<HistoryRecord> ViewModelHistory => _clipboardService.History;

    public MonitorPage()
    {
        InitializeComponent();

        var app = (App)Application.Current;
        _clipboardService = app.ClipboardService;
        _localizationService = app.LocalizationService;

        Loaded += MonitorPage_Loaded;
        Unloaded += MonitorPage_Unloaded;

        ApplyLocalization();
        _localizationService.LanguageChanged += () => DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    public void ApplyLocalization()
    {
        MonitorPageTitle.Text = _localizationService["MonitorTitle"];
        MonitorPageSubtitle.Text = _localizationService["MonitorSubtitle"];
        MonitoringToggle.OnContent = _localizationService["StatusMonitoringActive"];
        MonitoringToggle.OffContent = _localizationService["StatusMonitoringPaused"];
        ClearHistoryButton.Content = _localizationService["ButtonClearHistory"];

        MetricTotalLabel.Text = _localizationService["StatTotalEvents"];
        MetricCleanedLabel.Text = _localizationService["StatCleaned"];
        MetricAvgTimeLabel.Text = _localizationService["StatLastActivity"];

        OperationsLogHeader.Text = _localizationService["OperationsLogHeader"];
        EmptyStateTitle.Text = _localizationService["EmptyLogTitle"];
        EmptyStateSubtitle.Text = _localizationService["EmptyLogSubtitle"];
    }

    private void MonitorPage_Loaded(object sender, RoutedEventArgs e)
    {
        MonitoringToggle.IsOn = _clipboardService.IsMonitoringEnabled;

        if (!_isSubscribed)
        {
            _clipboardService.StatsChanged += OnStatsChanged;
            _clipboardService.History.CollectionChanged += OnHistoryCollectionChanged;
            _isSubscribed = true;
        }

        UpdateStatsView();
        UpdateEmptyState();
    }

    private void MonitorPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_isSubscribed)
        {
            _clipboardService.StatsChanged -= OnStatsChanged;
            _clipboardService.History.CollectionChanged -= OnHistoryCollectionChanged;
            _isSubscribed = false;
        }
    }

    private void OnStatsChanged(object? sender, EventArgs e)
    {
        UpdateStatsView();
    }

    private void OnHistoryCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyState();
    }

    private void UpdateStatsView()
    {
        TotalCountText.Text = _clipboardService.TotalProcessedCount.ToString(CultureInfo.InvariantCulture);
        TotalCharsText.Text = _clipboardService.TotalCharsCleanedCount.ToString(CultureInfo.InvariantCulture);
        LastActivityText.Text = _clipboardService.LastOperationTimestamp?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "-";
    }

    private void UpdateEmptyState()
    {
        EmptyStateBorder.Visibility = _clipboardService.History.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryListView.Visibility = _clipboardService.History.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MonitoringToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _clipboardService.IsMonitoringEnabled = MonitoringToggle.IsOn;
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        _clipboardService.ClearHistory();
        UpdateEmptyState();
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HistoryRecord item })
        {
            try
            {
                _ = ClipboardService.RestoreOriginal(item);
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
            {
                // Clipboard may be temporarily locked
            }
        }
    }

    private void CopyResultButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HistoryRecord item })
        {
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(item.CleanedText);
                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
            {
                // Clipboard may be temporarily locked
            }
        }
    }
}
