using System;
using System.Collections.Generic;
using System.Linq;
using ClearAIText.Windows.Clipboard;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace ClearAIText.Windows.Process;

public record RunningAppInfo(string ProcessName, string WindowTitle, string ExeName);

public static class RunningAppsDetector
{
    private const int MaxStackAllocLength = 512;

    public static IReadOnlyList<RunningAppInfo> GetRunningWindowsApps()
    {
        var apps = new Dictionary<string, RunningAppInfo>(StringComparer.OrdinalIgnoreCase);

        unsafe
        {
            _ = PInvoke.EnumWindows((hwnd, lParam) =>
            {
                if (!PInvoke.IsWindowVisible(hwnd))
                {
                    return true;
                }

                int length = PInvoke.GetWindowTextLength(hwnd);
                if (length <= 0 || length > 4096)
                {
                    return true;
                }

                Span<char> titleBuffer = length < MaxStackAllocLength
                    ? stackalloc char[length + 2]
                    : new char[length + 2];

                fixed (char* pTitle = titleBuffer)
                {
                    int readLen = PInvoke.GetWindowText(hwnd, pTitle, length + 1);
                    if (readLen <= 0)
                    {
                        return true;
                    }

                    string title = new string(pTitle, 0, readLen).Trim();
                    if (string.IsNullOrWhiteSpace(title) ||
                        title == "Program Manager" ||
                        title == "Default IME" ||
                        title == "MSCTFIME UI" ||
                        title == "Clear AI Text")
                    {
                        return true;
                    }

                    string? processName = ProcessDetector.GetProcessNameFromHwnd(hwnd);
                    if (string.IsNullOrWhiteSpace(processName) ||
                        processName.Equals("explorer", StringComparison.OrdinalIgnoreCase) ||
                        processName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) ||
                        processName.Equals("Clear-AI-Text", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (!apps.ContainsKey(processName))
                    {
                        apps[processName] = new RunningAppInfo(
                            ProcessName: processName,
                            WindowTitle: title,
                            ExeName: $"{processName}.exe"
                        );
                    }
                }

                return true;
            }, default);
        }

        return apps.Values.OrderBy(a => a.WindowTitle, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
