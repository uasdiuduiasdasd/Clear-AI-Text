using System.Globalization;
using System.Text;
using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Tier 2 Rule: Strips diacritics and accent marks while preserving linguistic letters such as Cyrillic 'ё' and 'й'.
/// </summary>
public sealed class DiacriticsStripper : IRule
{
    public string Name => "Diacritics & Accents Stripper";
    public RuleTier Tier => RuleTier.Tier2Destructive;

    public (string Output, int Replacements) Apply(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (input, 0);
        }

        string decomposed = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        int replacements = 0;

        for (int i = 0; i < decomposed.Length; i++)
        {
            char c = decomposed[i];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                // Invariant check: Preserve Cyrillic letters:
                // Russian 'ё' (е + \u0308), 'й' (и + \u0306)
                // Ukrainian 'ї' (і + \u0308)
                // Belarusian 'ў' (у + \u0306)
                if (i > 0)
                {
                    char prev = decomposed[i - 1];
                    if ((prev is 'е' or 'Е' or 'і' or 'І' or '\u0456' or '\u0406') && c == '\u0308') // Diaeresis for ё, ї
                    {
                        sb.Append(c);
                        continue;
                    }
                    if ((prev is 'и' or 'И' or 'у' or 'У' or '\u0443' or '\u0423') && c == '\u0306') // Breve for й, ў
                    {
                        sb.Append(c);
                        continue;
                    }
                }

                // Strip this combining mark (accent/stress/tilde/etc.)
                replacements++;
            }
            else
            {
                sb.Append(c);
            }
        }

        if (replacements == 0)
        {
            return (input, 0);
        }

        string result = sb.ToString().Normalize(NormalizationForm.FormC);
        return (result, replacements);
    }
}
