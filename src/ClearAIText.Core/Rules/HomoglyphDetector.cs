using System.Text.RegularExpressions;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Tier 3 Heuristic: Identifies mixed-script confusable homoglyphs (e.g., Latin characters visually spoofing Cyrillic letters).
/// </summary>
public sealed partial class HomoglyphDetector
{
    // Match word boundaries containing word characters with bounded timeout
    [GeneratedRegex(@"\b[\p{L}\p{M}]+\b", RegexOptions.Compiled, matchTimeoutMilliseconds: 100)]
    private static partial Regex WordRegex();

    public static IReadOnlyList<string> Analyze(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var confusables = new List<string>();
        try
        {
            var matches = WordRegex().Matches(input);

            foreach (Match match in matches)
            {
                string word = match.Value;
                if (HasMixedScripts(word))
                {
                    confusables.Add(word);
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return [];
        }

        return confusables;
    }

    private static bool HasMixedScripts(string word)
    {
        bool hasCyrillic = false;
        bool hasLatin = false;

        foreach (char c in word)
        {
            if (IsCyrillic(c))
            {
                hasCyrillic = true;
            }
            else if (IsLatin(c))
            {
                hasLatin = true;
            }

            if (hasCyrillic && hasLatin)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCyrillic(char c) =>
        (c >= '\u0400' && c <= '\u04FF') || // Cyrillic
        (c >= '\u0500' && c <= '\u052F') || // Cyrillic Supplementary
        (c >= '\u2DE0' && c <= '\u2DFF') || // Cyrillic Extended-A
        (c >= '\uA640' && c <= '\uA69F');   // Cyrillic Extended-B

    private static bool IsLatin(char c) =>
        (c >= 'a' && c <= 'z') ||
        (c >= 'A' && c <= 'Z') ||
        (c >= '\u00C0' && c <= '\u024F');   // Latin-1 Supplement + Latin Extended
}
