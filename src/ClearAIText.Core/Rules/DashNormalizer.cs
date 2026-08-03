using System.Text;
using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Tier 1 Rule: Standardizes typographic dashes and hyphens to standard ASCII hyphen '-'.
/// </summary>
public sealed class DashNormalizer : IRule
{
    public string Name => "Dash Normalizer";
    public RuleTier Tier => RuleTier.Tier1Safe;

    public (string Output, int Replacements) Apply(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (input, 0);
        }

        int replacementCount = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (IsTargetDash(input[i]))
            {
                replacementCount++;
            }
        }

        if (replacementCount == 0)
        {
            return (input, 0);
        }

        var sb = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (IsTargetDash(c))
            {
                sb.Append('-');
            }
            else
            {
                sb.Append(c);
            }
        }

        return (sb.ToString(), replacementCount);
    }

    private static bool IsTargetDash(char c) => c switch
    {
        '\u2014' => true, // Em Dash (—)
        '\u2013' => true, // En Dash (–)
        '\u2015' => true, // Horizontal Bar (―)
        '\u2012' => true, // Figure Dash (‒)
        '\u2212' => true, // Minus Sign (−)
        '\u2010' => true, // Hyphen (‐)
        '\u2011' => true, // Non-breaking Hyphen (‑)
        '\uFE58' => true, // Small Em Dash
        '\uFE63' => true, // Small Hyphen-Minus
        '\uFF0D' => true, // Fullwidth Hyphen-Minus
        _ => false
    };
}
