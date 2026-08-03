namespace ClearAIText.Core.Models;

/// <summary>
/// Represents a user-configured text replacement rule.
/// </summary>
public sealed record CustomRule
{
    public string Name { get; init; } = string.Empty;
    public string FindPattern { get; init; } = string.Empty;
    public string Replacement { get; init; } = string.Empty;
    public bool IsRegex { get; init; }
    public bool IsEnabled { get; init; } = true;
}
