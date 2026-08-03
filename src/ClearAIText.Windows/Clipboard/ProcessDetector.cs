using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace ClearAIText.Windows.Clipboard;

/// <summary>
/// Identifies source application processes and evaluates process exclusion rules.
/// </summary>
public sealed class ProcessDetector
{
    private readonly Lock _lock = new();
    private readonly HashSet<string> _excludedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "KeePass",
        "1Password",
        "Bitwarden",
        "Enpass",
        "NordPass",
        "LastPass"
    };

    public IReadOnlyCollection<string> ExcludedProcesses
    {
        get
        {
            lock (_lock)
            {
                return _excludedProcesses.ToList();
            }
        }
    }

    public void AddExcludedProcess(string processName)
    {
        if (!string.IsNullOrWhiteSpace(processName))
        {
            lock (_lock)
            {
                _ = _excludedProcesses.Add(processName.Trim());
            }
        }
    }

    public void RemoveExcludedProcess(string processName)
    {
        if (!string.IsNullOrWhiteSpace(processName))
        {
            lock (_lock)
            {
                _ = _excludedProcesses.Remove(processName.Trim());
            }
        }
    }

    public void UpdateExcludedProcesses(IEnumerable<string> processes)
    {
        lock (_lock)
        {
            _excludedProcesses.Clear();
            foreach (string p in processes)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    _ = _excludedProcesses.Add(p.Trim());
                }
            }
        }
    }

    /// <summary>
    /// Obtains the process name of the current clipboard owner or foreground window.
    /// </summary>
    public static string? GetSourceProcessName()
    {
        HWND hwnd = PInvoke.GetClipboardOwner();
        if (hwnd.IsNull)
        {
            hwnd = PInvoke.GetForegroundWindow();
        }

        if (hwnd.IsNull)
        {
            return null;
        }

        return GetProcessNameFromHwnd(hwnd);
    }

    public static string? GetProcessNameFromHwnd(nint hwnd) => GetProcessNameFromHwnd((HWND)hwnd);

    internal static string? GetProcessNameFromHwnd(HWND hwnd)
    {
        if (hwnd.IsNull)
        {
            return null;
        }

        unsafe
        {
            uint processId = 0;
            _ = PInvoke.GetWindowThreadProcessId(hwnd, &processId);
            if (processId == 0)
            {
                return null;
            }

            try
            {
                using var hProcess = PInvoke.OpenProcess_SafeHandle(
                    global::Windows.Win32.System.Threading.PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION,
                    false,
                    processId);

                if (!hProcess.IsInvalid)
                {
                    Span<char> buffer = stackalloc char[512];
                    fixed (char* pBuffer = buffer)
                    {
                        uint size = (uint)buffer.Length;
                        if (PInvoke.QueryFullProcessImageName(hProcess, 0, new PWSTR(pBuffer), ref size) && size > 0)
                        {
                            ReadOnlySpan<char> path = buffer[..(int)size];
                            int lastSlash = path.LastIndexOfAny(['\\', '/']);
                            ReadOnlySpan<char> fileName = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
                            if (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                fileName = fileName[..^4];
                            }
                            return fileName.ToString();
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Fallback to managed Process if native API fails
            }

            try
            {
                using var proc = System.Diagnostics.Process.GetProcessById((int)processId);
                return proc.ProcessName;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Determines whether the specified process should be excluded from automated text cleaning.
    /// </summary>
    public bool IsProcessExcluded(string? processName)
    {
        if (string.IsNullOrEmpty(processName))
        {
            return false;
        }

        lock (_lock)
        {
            return _excludedProcesses.Contains(processName);
        }
    }
}
