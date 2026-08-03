using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;

namespace ClearAIText.Windows.Clipboard;

/// <summary>
/// RAII session manager for native Win32 clipboard locking with bounded exponential retry backoff.
/// </summary>
public sealed class ClipboardSession : IDisposable
{
    private const uint CF_UNICODETEXT = 13;
    private static readonly int[] RetryDelaysMs = [0, 5, 15, 35, 75];

    private readonly HWND _ownerHwnd;
    private bool _isOpen;

    private ClipboardSession(HWND ownerHwnd)
    {
        _ownerHwnd = ownerHwnd;
        _isOpen = true;
    }

    /// <summary>
    /// Attempts to open the Win32 clipboard using bounded exponential retry backoff.
    /// </summary>
    public static bool TryOpen(nint ownerHwnd, [NotNullWhen(true)] out ClipboardSession? session)
    {
        unsafe
        {
            HWND hwnd = new((void*)ownerHwnd);
            foreach (int delay in RetryDelaysMs)
            {
                if (delay > 0)
                {
                    Thread.Sleep(delay);
                }

                if (PInvoke.OpenClipboard(hwnd))
                {
                    session = new ClipboardSession(hwnd);
                    return true;
                }
            }

            session = null;
            return false;
        }
    }

    internal static bool TryOpen(HWND ownerHwnd, [NotNullWhen(true)] out ClipboardSession? session)
    {
        foreach (int delay in RetryDelaysMs)
        {
            if (delay > 0)
            {
                Thread.Sleep(delay);
            }

            if (PInvoke.OpenClipboard(ownerHwnd))
            {
                session = new ClipboardSession(ownerHwnd);
                return true;
            }
        }

        session = null;
        return false;
    }

    /// <summary>
    /// Safely reads the current unicode text (CF_UNICODETEXT) from the clipboard.
    /// </summary>
    public unsafe string? GetUnicodeText()
    {
        if (!_isOpen)
        {
            return null;
        }

        HANDLE handle = PInvoke.GetClipboardData(CF_UNICODETEXT);
        if (handle.IsNull)
        {
            return null;
        }

        HGLOBAL hGlobal = new((void*)handle.Value);
        void* ptr = PInvoke.GlobalLock(hGlobal);
        if (ptr == null)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUni((nint)ptr);
        }
        finally
        {
            _ = PInvoke.GlobalUnlock(hGlobal);
        }
    }

    /// <summary>
    /// Empties the clipboard and writes normalized text along with the internal loop-prevention marker.
    /// </summary>
    public unsafe bool SetUnicodeText(string text, uint internalMarkerFormatId)
    {
        if (!_isOpen)
        {
            return false;
        }

        if (!PInvoke.EmptyClipboard())
        {
            return false;
        }

        // 1. Allocate and set CF_UNICODETEXT
        int byteCount = (text.Length + 1) * sizeof(char);
        HGLOBAL hTextMem = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)byteCount);
        if (hTextMem.IsNull)
        {
            return false;
        }

        void* textPtr = PInvoke.GlobalLock(hTextMem);
        if (textPtr == null)
        {
            _ = PInvoke.GlobalFree(hTextMem);
            return false;
        }

        try
        {
            fixed (char* pSrc = text)
            {
                Buffer.MemoryCopy(pSrc, textPtr, byteCount, (long)text.Length * sizeof(char));
            }
            // Null terminator at the end
            ((char*)textPtr)[text.Length] = '\0';
        }
        finally
        {
            _ = PInvoke.GlobalUnlock(hTextMem);
        }

        HANDLE hResult = PInvoke.SetClipboardData(CF_UNICODETEXT, new HANDLE((void*)hTextMem.Value));
        if (hResult.IsNull)
        {
            _ = PInvoke.GlobalFree(hTextMem);
            return false;
        }

        // 2. Allocate and set ClearAIText.InternalMarker format payload
        if (internalMarkerFormatId != 0)
        {
            HGLOBAL hMarkerMem = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, 4);
            if (!hMarkerMem.IsNull)
            {
                void* markerPtr = PInvoke.GlobalLock(hMarkerMem);
                if (markerPtr != null)
                {
                    Marshal.WriteInt32((nint)markerPtr, 1);
                    _ = PInvoke.GlobalUnlock(hMarkerMem);
                    HANDLE hMarkerResult = PInvoke.SetClipboardData(internalMarkerFormatId, new HANDLE((void*)hMarkerMem.Value));
                    if (hMarkerResult.IsNull)
                    {
                        _ = PInvoke.GlobalFree(hMarkerMem);
                    }
                }
                else
                {
                    _ = PInvoke.GlobalFree(hMarkerMem);
                }
            }
        }

        return true;
    }

    public void Dispose()
    {
        if (_isOpen)
        {
            _ = PInvoke.CloseClipboard();
            _isOpen = false;
        }
    }
}
