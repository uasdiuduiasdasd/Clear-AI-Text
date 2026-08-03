using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace ClearAIText.Windows.Theme;

public static class WindowThemeHelper
{
    private const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public static unsafe void SetImmersiveDarkMode(nint hWnd, bool enable)
    {
        if (hWnd == 0)
        {
            return;
        }

        int useDarkMode = enable ? 1 : 0;
        _ = PInvoke.DwmSetWindowAttribute(
            new HWND((void*)hWnd),
            (DWMWINDOWATTRIBUTE)DWMWA_USE_IMMERSIVE_DARK_MODE,
            &useDarkMode,
            (uint)sizeof(int));
    }
}
