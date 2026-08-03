using System.Collections.ObjectModel;
using ClearAIText.App.Models;
using ClearAIText.Core.Pipeline;
using ClearAIText.Windows.Clipboard;
using Microsoft.UI.Dispatching;

namespace ClearAIText.App.Services;

/// <summary>
/// Central service managing clipboard events, history log, and pipeline coordination.
/// </summary>
public sealed class ClipboardService : IDisposable
{
    private readonly ClipboardListener _listener;
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _localizationService;
    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<HistoryRecord> History { get; } = [];

    public bool IsMonitoringEnabled
    {
        get => _listener.IsEnabled;
        set
        {
            _listener.IsEnabled = value;
            MonitoringStatusChanged?.Invoke(this, value);
        }
    }

    public int TotalProcessedCount { get; private set; }
    public int TotalCharsCleanedCount { get; private set; }
    public DateTime? LastOperationTimestamp { get; private set; }

    public event EventHandler<bool>? MonitoringStatusChanged;
    public event EventHandler? StatsChanged;

    public ClipboardService(ITextPipeline pipeline, SettingsService settingsService, LocalizationService localizationService, DispatcherQueue dispatcherQueue)
    {
        _settingsService = settingsService;
        _localizationService = localizationService;
        _dispatcherQueue = dispatcherQueue;

        var processDetector = new ProcessDetector();
        foreach (string proc in _settingsService.Settings.ExcludedProcesses)
        {
            processDetector.AddExcludedProcess(proc);
        }

        _listener = new ClipboardListener(pipeline, processDetector)
        {
            Profile = _settingsService.Settings.Profile
        };

        _listener.Sanitized += OnClipboardSanitized;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    public void Start()
    {
        _listener.Start();
    }

    public void Stop()
    {
        _listener.Stop();
    }

    public void ClearHistory()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            History.Clear();
            TotalProcessedCount = 0;
            TotalCharsCleanedCount = 0;
            LastOperationTimestamp = null;
            StatsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    public static bool RestoreOriginal(HistoryRecord item)
    {
        if (ClipboardSession.TryOpen(0, out var session))
        {
            using (session)
            {
                return session.SetUnicodeText(item.OriginalText, 0);
            }
        }

        return false;
    }

    private void OnClipboardSanitized(object? sender, ClipboardSanitizeEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            TotalProcessedCount++;
            int charsSaved = Math.Max(0, e.OriginalText.Length - e.Result.OutputText.Length);
            TotalCharsCleanedCount += charsSaved;
            LastOperationTimestamp = e.Timestamp.ToLocalTime();

            var historyItem = new HistoryRecord
            {
                Timestamp = e.Timestamp.ToLocalTime(),
                OriginalText = e.OriginalText,
                CleanedText = e.Result.OutputText,
                SourceProcess = e.SourceProcess ?? _localizationService["SystemClipboard"],
                AppliedRules = e.Result.AppliedRules
            };

            History.Insert(0, historyItem);

            // Limit history size to 50 items
            while (History.Count > 50)
            {
                History.RemoveAt(History.Count - 1);
            }

            StatsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _listener.Profile = _settingsService.Settings.Profile;
        _listener.ProcessDetector.UpdateExcludedProcesses(_settingsService.Settings.ExcludedProcesses);
    }

    public void Dispose()
    {
        _listener.Sanitized -= OnClipboardSanitized;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _listener.Dispose();
    }
}
