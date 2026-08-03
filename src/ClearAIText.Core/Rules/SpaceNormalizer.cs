using System.Text;
using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Tier 1 Rule: Standardizes non-breaking, thin, em, quad, and ideographic spaces to standard ASCII space ' '.
/// </summary>
public sealed class SpaceNormalizer : IRule
{
    public string Name => "Space Normalizer";
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
            if (IsNonStandardSpace(input[i]))
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
            if (IsNonStandardSpace(c))
            {
                sb.Append(' ');
            }
            else
            {
                sb.Append(c);
            }
        }

        return (sb.ToString(), replacementCount);
    }

    private static bool IsNonStandardSpace(char c) => c switch
    {
        '\u00A0' => true, // Non-breaking Space (NBSP)
        '\u202F' => true, // Narrow No-Break Space (NNBSP)
        '\u2000' => true, // En Quad
        '\u2001' => true, // Em Quad
        '\u2002' => true, // En Space
        '\u2003' => true, // Em Space
        '\u2004' => true, // Three-Per-Em Space
        '\u2005' => true, // Four-Per-Em Space
        '\u2006' => true, // Six-Per-Em Space
        '\u2007' => true, // Figure Space
        '\u2008' => true, // Punctuation Space
        '\u2009' => true, // Thin Space
        '\u200A' => true, // Hair Space
        '\u205F' => true, // Medium Mathematical Space
        '\u3000' => true, // Ideographic Space
        '\u180E' => true, // Mongolian Vowel Separator
        _ => false
    };
}
