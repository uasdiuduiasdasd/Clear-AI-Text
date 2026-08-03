using System.Globalization;
using System.Text;
using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Tier 2 Rule: Strips emoji sequences and pictographs using grapheme cluster analysis.
/// </summary>
public sealed class EmojiStripper : IRule
{
    public string Name => "Emoji & Pictograph Stripper";
    public RuleTier Tier => RuleTier.Tier2Destructive;

    public (string Output, int Replacements) Apply(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (input, 0);
        }

        var sb = new StringBuilder(input.Length);
        int replacements = 0;

        var enumerator = StringInfo.GetTextElementEnumerator(input);
        while (enumerator.MoveNext())
        {
            string element = enumerator.GetTextElement();
            if (IsEmojiGraphemeCluster(element))
            {
                replacements++;
            }
            else
            {
                sb.Append(element);
            }
        }

        if (replacements == 0)
        {
            return (input, 0);
        }

        return (sb.ToString(), replacements);
    }

    private static bool IsEmojiGraphemeCluster(string element)
    {
        // An emoji grapheme cluster must contain at least one primary emoji base code point.
        for (int i = 0; i < element.Length; i++)
        {
            int codePoint = char.ConvertToUtf32(element, i);
            if (char.IsSurrogatePair(element, i))
            {
                i++; // Skip low surrogate
            }

            if (IsEmojiBaseCodePoint(codePoint))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEmojiBaseCodePoint(int codePoint) => codePoint switch
    {
        >= 0x1F600 and <= 0x1F64F => true, // Emoticons
        >= 0x1F300 and <= 0x1F5FF => true, // Misc Symbols, Pictographs, and Modifiers
        >= 0x1F680 and <= 0x1F6FF => true, // Transport and Map
        >= 0x1F900 and <= 0x1F9FF => true, // Supplemental Symbols and Pictographs
        >= 0x1FA70 and <= 0x1FAFF => true, // Symbols and Pictographs Extended-A
        >= 0x2600 and <= 0x26FF => true,   // Misc Symbols
        >= 0x2700 and <= 0x27BF => true,   // Dingbats
        >= 0x1F1E6 and <= 0x1F1FF => true, // Regional Indicator Symbols (Flags)
        >= 0x231A and <= 0x231B => true,   // Watch, Hourglass
        >= 0x23E9 and <= 0x23F3 => true,   // Media controls
        >= 0x23F8 and <= 0x23FA => true,   // Audio / video buttons
        0x25AA or 0x25AB or 0x25FB or 0x25FC or 0x25FD or 0x25FE => true, // Geometric shapes
        _ => false
    };
}
