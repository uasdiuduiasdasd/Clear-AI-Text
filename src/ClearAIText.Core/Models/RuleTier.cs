namespace ClearAIText.Core.Models;

/// <summary>
/// Specifies the safety level and architectural tier of a text transformation rule.
/// </summary>
public enum RuleTier
{
    /// <summary>
    /// Tier 1: Non-destructive typographical standardizations (enabled by default).
    /// </summary>
    Tier1Safe = 1,

    /// <summary>
    /// Tier 2: Explicitly opt-in transformations that may remove linguistic or structural info (diacritics, emojis, markdown).
    /// </summary>
    Tier2Destructive = 2,

    /// <summary>
    /// Tier 3: Heuristic inspection rules that detect ambiguities (e.g. confusable homoglyphs).
    /// </summary>
    Tier3Heuristic = 3,

    /// <summary>
    /// User-defined custom literal or regular expression rules.
    /// </summary>
    Custom = 4
}
