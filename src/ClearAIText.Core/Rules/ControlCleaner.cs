using System.Text;
using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Tier 1 Rule: Removes leading BOM, soft hyphens, zero-width spaces, bidi isolates, unicode tag characters, and normalizes horizontal ellipses.
/// </summary>
public sealed class ControlCleaner : IRule
{
    public string Name => "Control & Invisible Cleaner";
    public RuleTier Tier => RuleTier.Tier1Safe;

    private readonly bool _normalizeEllipses;

    public ControlCleaner(bool normalizeEllipses = true)
    {
        _normalizeEllipses = normalizeEllipses;
    }

    public (string Output, int Replacements) Apply(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (input, 0);
        }

        int replacementCount = 0;
        bool hasLeadingBom = input[0] == '\uFEFF';
        if (hasLeadingBom)
        {
            replacementCount++;
        }

        int startIndex = hasLeadingBom ? 1 : 0;
        for (int i = startIndex; i < input.Length; i++)
        {
            char c = input[i];

            // Check for Unicode Tag characters (U+E0000 to U+E007F): surrogate pair (\uDB40 \uDC00..\uDC7F)
            if (i < input.Length - 1 && c == '\uDB40' && input[i + 1] >= '\uDC00' && input[i + 1] <= '\uDC7F')
            {
                replacementCount++;
                i++; // Skip low surrogate in loop count
                continue;
            }

            if (IsInvisibleArtifact(c) || (_normalizeEllipses && c == '\u2026'))
            {
                replacementCount++;
            }
        }

        if (replacementCount == 0 && !hasLeadingBom)
        {
            return (input, 0);
        }

        var sb = new StringBuilder(input.Length);
        for (int i = startIndex; i < input.Length; i++)
        {
            char c = input[i];

            // Strip Unicode Tag surrogate pair
            if (i < input.Length - 1 && c == '\uDB40' && input[i + 1] >= '\uDC00' && input[i + 1] <= '\uDC7F')
            {
                i++;
                continue;
            }

            if (IsInvisibleArtifact(c))
            {
                continue;
            }

            if (_normalizeEllipses && c == '\u2026')
            {
                sb.Append("...");
            }
            else
            {
                sb.Append(c);
            }
        }

        return (sb.ToString(), replacementCount);
    }

    private static bool IsInvisibleArtifact(char c) => c switch
    {
        '\u00AD' => true, // Soft Hyphen
        '\u200B' => true, // Zero Width Space (ZWSP)
        '\u2060' => true, // Word Joiner
        '\u200E' => true, // Left-to-Right Mark
        '\u200F' => true, // Right-to-Left Mark
        '\u202A' => true, // Left-to-Right Embedding
        '\u202B' => true, // Right-to-Left Embedding
        '\u202C' => true, // Pop Directional Formatting
        '\u202D' => true, // Left-to-Right Override
        '\u202E' => true, // Right-to-Left Override
        '\u2066' => true, // Left-to-Right Isolate
        '\u2067' => true, // Right-to-Left Isolate
        '\u2068' => true, // First Strong Isolate
        '\u2069' => true, // Pop Directional Isolate
        _ => false
    };
}
