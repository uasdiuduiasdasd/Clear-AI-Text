using System.Runtime.InteropServices;
using ClearAIText.Core.Models;
using ClearAIText.Core.Pipeline;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ClearAIText.Windows.Clipboard;

/// <summary>
/// Background Win32 clipboard format listener with loop prevention and process detection.
/// </summary>
public sealed class ClipboardListener : IDisposable
{
    private const uint WM_CLOSE = 0x0010;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_CLIPBOARDUPDATE = 0x031D;
    private const string WindowClassName = "ClearAIText_Clipboard_Msg_Wnd";

    private readonly ITextPipeline _pipeline;
    private readonly ProcessDetector _processDetector;
    private readonly uint _markerFormatId;

    private Thread? _messageLoopThread;
    private HWND _hwnd;
    private bool _isRunning;
    private bool _isEnabled = true;
    private uint _lastSequenceNumber;

    private string? _lastOriginalText;
    private string? _lastCleanedText;

    private readonly WNDPROC _wndProcDelegate;

    public event EventHandler<ClipboardSanitizeEventArgs>? Sanitized;
    public NormalizationProfile Profile { get; set; } = NormalizationProfile.CreateSafeDefault();

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public ProcessDetector ProcessDetector => _processDetector;

    public ClipboardListener(ITextPipeline pipeline, ProcessDetector? processDetector = null)
    {
        _pipeline = pipeline;
        _processDetector = processDetector ?? new ProcessDetector();

        // Register custom loop prevention format
        _markerFormatId = PInvoke.RegisterClipboardFormat("ClearAIText.InternalMarker");

        // Keep delegate alive to avoid garbage collection
        _wndProcDelegate = WndProc;
    }

    /// <summary>
    /// Starts the background STA message thread and registers the clipboard format listener.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        using var initEvent = new ManualResetEvent(false);

        _messageLoopThread = new Thread(() =>
        {
            InitializeMessageWindow();
            _ = initEvent.Set();

            // Win32 message pump
            while (_isRunning && PInvoke.GetMessage(out MSG msg, HWND.Null, 0, 0))
            {
                _ = PInvoke.TranslateMessage(msg);
                _ = PInvoke.DispatchMessage(msg);
            }

            unsafe
            {
                fixed (char* pClassName = WindowClassName)
                {
                    _ = PInvoke.UnregisterClass(pClassName, default);
                }
            }
        })
        {
            IsBackground = true,
            Name = "ClearAIText_Clipboard_Thread"
        };

        _messageLoopThread.SetApartmentState(ApartmentState.STA);
        _messageLoopThread.Start();

        _ = initEvent.WaitOne(3000);
    }

    /// <summary>
    /// Stops the clipboard format listener and destroys the message window.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        if (!_hwnd.IsNull)
        {
            _ = PInvoke.RemoveClipboardFormatListener(_hwnd);
            _ = PInvoke.DestroyWindow(_hwnd);
            _hwnd = HWND.Null;
        }

        _messageLoopThread?.Join(1000);
        _messageLoopThread = null;
    }

    /// <summary>
    /// Restores the previous raw clipboard text prior to sanitization.
    /// </summary>
    public bool UndoLastSanitization()
    {
        if (string.IsNullOrEmpty(_lastOriginalText))
        {
            return false;
        }

        if (ClipboardSession.TryOpen(_hwnd, out var session))
        {
            using (session)
            {
                bool success = session.SetUnicodeText(_lastOriginalText, _markerFormatId);
                if (success)
                {
                    _lastSequenceNumber = PInvoke.GetClipboardSequenceNumber();
                    _lastOriginalText = null;
                    return true;
                }
            }
        }

        return false;
    }

    private unsafe void InitializeMessageWindow()
    {
        fixed (char* pClassName = WindowClassName)
        {
            var wndClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = _wndProcDelegate,
                lpszClassName = pClassName
            };

            _ = PInvoke.RegisterClassEx(wndClass);

            _hwnd = PInvoke.CreateWindowEx(
                0,
                WindowClassName,
                "ClearAIText_HiddenWindow",
                0,
                0, 0, 0, 0,
                new HWND((void*)-3), // HWND_MESSAGE
                default,
                default,
                null);

            if (!_hwnd.IsNull)
            {
                _ = PInvoke.AddClipboardFormatListener(_hwnd);
                _lastSequenceNumber = PInvoke.GetClipboardSequenceNumber();
            }
        }
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_CLIPBOARDUPDATE && _isEnabled)
        {
            HandleClipboardUpdate();
            return new LRESULT(0);
        }

        if (msg == WM_DESTROY)
        {
            PInvoke.PostQuitMessage(0);
            return new LRESULT(0);
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private void HandleClipboardUpdate()
    {
        uint currentSeq = PInvoke.GetClipboardSequenceNumber();
        if (currentSeq == _lastSequenceNumber && _lastSequenceNumber != 0)
        {
            return;
        }

        if (!ClipboardSession.TryOpen(_hwnd, out var session))
        {
            return;
        }

        using (session)
        {
            // Loop prevention check: Is our custom internal marker format present?
            if (_markerFormatId != 0 && PInvoke.IsClipboardFormatAvailable(_markerFormatId))
            {
                _lastSequenceNumber = currentSeq;
                return;
            }

            // Excluded process check (e.g. password managers)
            string? sourceProcess = ProcessDetector.GetSourceProcessName();
            if (_processDetector.IsProcessExcluded(sourceProcess))
            {
                return;
            }

            string? rawText = session.GetUnicodeText();
            if (string.IsNullOrEmpty(rawText))
            {
                return;
            }

            var result = _pipeline.Process(rawText, Profile);
            if (!result.HasModifications)
            {
                return;
            }

            // Save original for Undo functionality
            _lastOriginalText = rawText;
            _lastCleanedText = result.OutputText;

            // Write back sanitized text with loop-prevention marker
            bool writeSuccess = session.SetUnicodeText(result.OutputText, _markerFormatId);
            if (writeSuccess)
            {
                _lastSequenceNumber = PInvoke.GetClipboardSequenceNumber();
                Sanitized?.Invoke(this, new ClipboardSanitizeEventArgs
                {
                    OriginalText = rawText,
                    Result = result,
                    SourceProcess = sourceProcess
                });
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
