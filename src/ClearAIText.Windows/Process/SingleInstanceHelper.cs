using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ClearAIText.Windows.Process;

/// <summary>
/// Provides utility methods for detecting and activating existing application instances.
/// </summary>
public static class SingleInstanceHelper
{
    public static bool TryActivateExistingWindow(string windowTitle)
    {
        if (string.IsNullOrEmpty(windowTitle))
        {
            return false;
        }

        unsafe
        {
            fixed (char* pTitle = windowTitle)
            {
                HWND hwnd = PInvoke.FindWindow(default(PCWSTR), pTitle);
                if (!hwnd.IsNull)
                {
                    _ = PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
                    _ = PInvoke.SetForegroundWindow(hwnd);
                    return true;
                }
            }
        }

        return false;
    }
}
