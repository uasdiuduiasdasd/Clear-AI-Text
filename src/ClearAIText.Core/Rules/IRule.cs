using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Defines the contract for an individual text transformation rule.
/// </summary>
public interface IRule
{
    /// <summary>
    /// Descriptive name of the rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Safety and classification tier.
    /// </summary>
    RuleTier Tier { get; }

    /// <summary>
    /// Applies the transformation rule to the input text.
    /// </summary>
    /// <param name="input">The raw text to process.</param>
    /// <returns>A tuple containing the transformed output string and count of replacements made.</returns>
    (string Output, int Replacements) Apply(string input);
}
