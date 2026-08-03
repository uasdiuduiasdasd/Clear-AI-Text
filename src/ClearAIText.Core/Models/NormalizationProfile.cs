namespace ClearAIText.Core.Models;

/// <summary>
/// Configuration profile governing which transformation rules are active.
/// </summary>
public sealed class NormalizationProfile
{
    public string Name { get; set; } = "Safe AI Clean";
    public bool IsEnabled { get; set; } = true;

    // Tier 1: Safe Normalization (Enabled by default)
    public bool ReplaceDashes { get; set; } = true;
    public bool ReplaceQuotes { get; set; } = true;
    public bool NormalizeSpaces { get; set; } = true;
    public bool CleanInvisibleControls { get; set; } = true;
    public bool NormalizeEllipses { get; set; } = true;

    // Tier 2: Destructive Transformations (Explicitly opt-in)
    public bool StripDiacritics { get; set; }
    public bool StripEmojis { get; set; }
    public bool StripLightMarkdown { get; set; }

    // Tier 3: Heuristic Inspection
    public bool DetectConfusables { get; set; }

    // Custom Rules
    public List<CustomRule> CustomRules { get; set; } = [];

    /// <summary>
    /// Creates the standard recommended profile for everyday clipboard sanitization.
    /// </summary>
    public static NormalizationProfile CreateSafeDefault() => new()
    {
        Name = "Safe AI Clean",
        ReplaceDashes = true,
        ReplaceQuotes = true,
        NormalizeSpaces = true,
        CleanInvisibleControls = true,
        NormalizeEllipses = true,
        StripDiacritics = false,
        StripEmojis = false,
        StripLightMarkdown = false,
        DetectConfusables = false
    };

    /// <summary>
    /// Creates an aggressive profile suitable for plain-text data extraction.
    /// </summary>
    public static NormalizationProfile CreateAggressiveClean() => new()
    {
        Name = "Aggressive Plain Text",
        ReplaceDashes = true,
        ReplaceQuotes = true,
        NormalizeSpaces = true,
        CleanInvisibleControls = true,
        NormalizeEllipses = true,
        StripDiacritics = true,
        StripEmojis = true,
        StripLightMarkdown = true,
        DetectConfusables = false
    };
}
