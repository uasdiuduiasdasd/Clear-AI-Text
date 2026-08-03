using ClearAIText.Core.Models;

namespace ClearAIText.Windows.Clipboard;

/// <summary>
/// Event arguments for clipboard sanitization events.
/// </summary>
public sealed class ClipboardSanitizeEventArgs : EventArgs
{
    public required string OriginalText { get; init; }
    public required NormalizationResult Result { get; init; }
    public string? SourceProcess { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
