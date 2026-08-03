using System.Text;
using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Tier 1 Rule: Standardizes typographic quotation marks, guillemets, and curled apostrophes to ASCII quotes.
/// </summary>
public sealed class QuoteNormalizer : IRule
{
    public string Name => "Quote Normalizer";
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
            if (TryNormalizeQuote(input[i], out _))
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
            if (TryNormalizeQuote(c, out char normalized))
            {
                sb.Append(normalized);
            }
            else
            {
                sb.Append(c);
            }
        }

        return (sb.ToString(), replacementCount);
    }

    private static bool TryNormalizeQuote(char c, out char normalized)
    {
        switch (c)
        {
            // Double quotes -> ASCII '"' (U+0022)
            case '\u00AB': // « (Left-pointing double angle quotation mark)
            case '\u00BB': // » (Right-pointing double angle quotation mark)
            case '\u201C': // “ (Left double quotation mark)
            case '\u201D': // ” (Right double quotation mark)
            case '\u201E': // „ (Double low-9 quotation mark)
            case '\u201F': // ‟ (Double high-reversed-9 quotation mark)
            case '\u2033': // ″ (Double prime)
            case '\u300C': // 「 (CJK Left corner bracket)
            case '\u300D': // 」 (CJK Right corner bracket)
            case '\u300A': // 《 (CJK Left double angle bracket)
            case '\u300B': // 》 (CJK Right double angle bracket)
            case '\uFF02': // ＂ (Fullwidth quotation mark)
                normalized = '"';
                return true;

            // Single quotes / apostrophes -> ASCII '\'' (U+0027)
            case '\u2018': // ‘ (Left single quotation mark)
            case '\u2019': // ’ (Right single quotation mark)
            case '\u201A': // ‚ (Single low-9 quotation mark)
            case '\u201B': // ‛ (Single high-reversed-9 quotation mark)
            case '\u00B4': // ´ (Acute accent)
            case '\u2032': // ′ (Prime)
            case '\u2039': // ‹ (Single left-pointing angle quotation mark)
            case '\u203A': // › (Single right-pointing angle quotation mark)
            case '\uFF07': // ＇ (Fullwidth apostrophe)
                normalized = '\'';
                return true;

            default:
                normalized = c;
                return false;
        }
    }
}
