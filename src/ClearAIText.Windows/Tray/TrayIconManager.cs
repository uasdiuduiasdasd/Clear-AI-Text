using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ClearAIText.Windows.Tray;

/// <summary>
/// Manages the native Windows notification area (System Tray) icon lifecycle, context menu, and events.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private const uint WM_TRAYICON = 0x8001;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint TRAY_ICON_ID = 1001;

    private const uint IDM_OPEN = 2001;
    private const uint IDM_TOGGLE_MONITORING = 2002;
    private const uint IDM_SANDBOX = 2003;
    private const uint IDM_SETTINGS = 2004;
    private const uint IDM_EXIT = 2005;

    private readonly HWND _messageHwnd;
    private readonly Action _onRestoreRequested;
    private readonly Action _onExitRequested;
    private readonly Action _onNavigateSandboxRequested;
    private readonly Action _onNavigateSettingsRequested;
    private readonly Func<bool> _isMonitoringActiveGetter;
    private readonly Action<bool> _onToggleMonitoringRequested;
    private readonly Func<string, string>? _stringGetter;
    private readonly WNDPROC _wndProcDelegate;
    private HICON _hIcon;
    private bool _isIconAdded;

    public TrayIconManager(
        Action onRestoreRequested,
        Action onExitRequested,
        Action onNavigateSandboxRequested,
        Action onNavigateSettingsRequested,
        Func<bool> isMonitoringActiveGetter,
        Action<bool> onToggleMonitoringRequested,
        Func<string, string>? stringGetter = null)
    {
        _onRestoreRequested = onRestoreRequested;
        _onExitRequested = onExitRequested;
        _onNavigateSandboxRequested = onNavigateSandboxRequested;
        _onNavigateSettingsRequested = onNavigateSettingsRequested;
        _isMonitoringActiveGetter = isMonitoringActiveGetter;
        _onToggleMonitoringRequested = onToggleMonitoringRequested;
        _stringGetter = stringGetter;

        _wndProcDelegate = WndProc;
        _messageHwnd = CreateMessageWindow();
        LoadTrayIconHandle();
        AddTrayIcon();
    }

    private unsafe void LoadTrayIconHandle()
    {
        string baseDir = AppContext.BaseDirectory;
        string icoPath = System.IO.Path.Combine(baseDir, "Assets", "app.ico");

        if (System.IO.File.Exists(icoPath))
        {
            fixed (char* pPath = icoPath)
            {
                int cx = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON);
                int cy = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSMICON);
                if (cx <= 0) cx = 16;
                if (cy <= 0) cy = 16;

                var hImg = PInvoke.LoadImage(
                    HINSTANCE.Null,
                    pPath,
                    GDI_IMAGE_TYPE.IMAGE_ICON,
                    cx,
                    cy,
                    IMAGE_FLAGS.LR_LOADFROMFILE);

                if (!hImg.IsNull)
                {
                    _hIcon = new HICON(hImg.Value);
                    return;
                }

                HICON hSmallIcon = default;
                uint extracted = PInvoke.ExtractIconEx(pPath, 0, null, &hSmallIcon, 1);
                if (extracted > 0 && !hSmallIcon.IsNull)
                {
                    _hIcon = hSmallIcon;
                    return;
                }
            }
        }

        // Fallback: extract from running process exe
        string? exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
        {
            fixed (char* pExe = exePath)
            {
                HICON hSmallIcon = default;
                uint extracted = PInvoke.ExtractIconEx(pExe, 0, null, &hSmallIcon, 1);
                if (extracted > 0 && !hSmallIcon.IsNull)
                {
                    _hIcon = hSmallIcon;
                }
            }
        }
    }

    private unsafe HWND CreateMessageWindow()
    {
        const string className = "ClearAIText_Tray_Message_Class";
        fixed (char* pClassName = className)
        {
            var wndClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = _wndProcDelegate,
                lpszClassName = pClassName
            };

            _ = PInvoke.RegisterClassEx(wndClass);

            return PInvoke.CreateWindowEx(
                0,
                className,
                "ClearAIText_TrayWindow",
                0,
                0, 0, 0, 0,
                new HWND((void*)-3), // HWND_MESSAGE
                default,
                default,
                null);
        }
    }

    private unsafe void AddTrayIcon()
    {
        if (_messageHwnd.IsNull || _isIconAdded)
        {
            return;
        }

        var flags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
        if (!_hIcon.IsNull)
        {
            flags |= NOTIFY_ICON_DATA_FLAGS.NIF_ICON;
        }

        var nid = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _messageHwnd,
            uID = TRAY_ICON_ID,
            uFlags = flags,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _hIcon
        };

        string tip = _stringGetter?.Invoke("AppName") ?? "Clear AI Text";
        fixed (char* pTip = tip)
        {
            int length = Math.Min(tip.Length, 127);
            for (int i = 0; i < length; i++)
            {
                nid.szTip[i] = pTip[i];
            }
            nid.szTip[length] = '\0';
        }

        if (PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, &nid))
        {
            _isIconAdded = true;
        }
    }

    private unsafe void RemoveTrayIcon()
    {
        if (!_isIconAdded || _messageHwnd.IsNull)
        {
            return;
        }

        var nid = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _messageHwnd,
            uID = TRAY_ICON_ID
        };

        _ = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, &nid);
        _isIconAdded = false;
    }

    private unsafe void ShowContextMenu()
    {
        using var hMenu = PInvoke.CreatePopupMenu_SafeHandle();
        if (hMenu.IsInvalid)
        {
            return;
        }

        bool isMonitoring = _isMonitoringActiveGetter?.Invoke() ?? true;

        string openLabel = _stringGetter?.Invoke("TrayShowWindow") ?? "Открыть";
        _ = PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_STRING, IDM_OPEN, openLabel);

        var monitorFlag = MENU_ITEM_FLAGS.MF_STRING;
        if (isMonitoring)
        {
            monitorFlag |= MENU_ITEM_FLAGS.MF_CHECKED;
        }

        string monitoringLabel = _stringGetter?.Invoke("TrayMonitoring") ?? "Нормализация";
        string activeState = isMonitoring
            ? (_stringGetter?.Invoke("ToggleEnabled") ?? "Включено")
            : (_stringGetter?.Invoke("ToggleDisabled") ?? "Отключено");
        string fullMonitorLabel = $"{monitoringLabel} ({activeState})";

        _ = PInvoke.AppendMenu(hMenu, monitorFlag, IDM_TOGGLE_MONITORING, fullMonitorLabel);

        _ = PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, string.Empty);
        string sandboxLabel = _stringGetter?.Invoke("TraySandbox") ?? "Песочница";
        _ = PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_STRING, IDM_SANDBOX, sandboxLabel);
        string settingsLabel = _stringGetter?.Invoke("TraySettings") ?? "Настройки";
        _ = PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_STRING, IDM_SETTINGS, settingsLabel);
        _ = PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, string.Empty);
        string exitLabel = _stringGetter?.Invoke("TrayExit") ?? "Выход";
        _ = PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_STRING, IDM_EXIT, exitLabel);

        _ = PInvoke.SetForegroundWindow(_messageHwnd);
        _ = PInvoke.GetCursorPos(out var pt);

        var cmd = PInvoke.TrackPopupMenuEx(
            hMenu,
            (uint)(TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD | TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY),
            pt.X,
            pt.Y,
            _messageHwnd,
            null);

        _ = PInvoke.PostMessage(_messageHwnd, 0, default, default);

        switch ((uint)cmd.Value)
        {
            case IDM_OPEN:
                _onRestoreRequested();
                break;
            case IDM_TOGGLE_MONITORING:
                _onToggleMonitoringRequested?.Invoke(!isMonitoring);
                break;
            case IDM_SANDBOX:
                _onNavigateSandboxRequested?.Invoke();
                break;
            case IDM_SETTINGS:
                _onNavigateSettingsRequested?.Invoke();
                break;
            case IDM_EXIT:
                _onExitRequested();
                break;
        }
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_TRAYICON)
        {
            uint mouseMsg = (uint)lParam.Value;
            if (mouseMsg == WM_LBUTTONUP)
            {
                _onRestoreRequested();
                return new LRESULT(0);
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                ShowContextMenu();
                return new LRESULT(0);
            }
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    public unsafe void Dispose()
    {
        RemoveTrayIcon();
        if (!_hIcon.IsNull)
        {
            _ = PInvoke.DestroyIcon(_hIcon);
            _hIcon = default;
        }

        if (!_messageHwnd.IsNull)
        {
            _ = PInvoke.DestroyWindow(_messageHwnd);
        }

        const string className = "ClearAIText_Tray_Message_Class";
        fixed (char* pClassName = className)
        {
            _ = PInvoke.UnregisterClass(pClassName, default);
        }
    }
}

