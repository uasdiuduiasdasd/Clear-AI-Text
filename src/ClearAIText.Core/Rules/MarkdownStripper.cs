using System.Text;
using System.Text.RegularExpressions;
using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// Tier 2 Rule: Strips markdown syntax (bold, italic, headers, lists, blockquotes, horizontal rules, inline code, links) while preserving fenced code blocks.
/// </summary>
public sealed class MarkdownStripper : IRule
{
    public string Name => "Markdown Stripper";
    public RuleTier Tier => RuleTier.Tier2Destructive;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    // Line Splitting regex preserving any newline convention (\r\n, \r, \n, \u2028, \u2029, \u0085)
    private static readonly Regex LineSplitRegex = new(@"(\r\n|\r|\n|\u2028|\u2029|\u0085)", RegexOptions.Compiled, RegexTimeout);

    // Structural Line-Prefix Regexes (applied to single lines)
    private static readonly Regex HorizontalRuleRegex = new(@"^\s{0,3}(?:(?:-[ \t]*){3,}|(?:\*[ \t]*){3,}|(?:_[ \t]*){3,})\s*$", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex TableDividerRegex = new(@"^\s*\|?(\s*:?-{2,}:?\s*\|)+\s*:?-{2,}:?\s*\|?\s*$", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex TaskListCheckboxRegex = new(@"^\s{0,3}(?:[*+-]|\d+[\.)])\s+\[[ xX]\]\s+", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex ListBulletRegex = new(@"^\s{0,3}[*+-]\s+", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex NumberedListRegex = new(@"^\s{0,3}\d+[\.)]\s+", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex HeaderRegex = new(@"^\s{0,3}#{1,6}\s+", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex TrailingHeaderHashesRegex = new(@"\s+#{1,6}\s*$", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex BlockquoteRegex = new(@"^\s{0,3}(?:>\s*)+", RegexOptions.Compiled, RegexTimeout);

    // Inline Formatting Regexes
    private static readonly Regex ImageRegex = new(@"!\[([^\]]*)\]\([^\)]+\)", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\([^\)]+\)", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex RefLinkRegex = new(@"\[([^\]]+)\]\[[^\]]*\]", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex AutoLinkRegex = new(@"<(https?://[^>]+)>", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex InlineCodeRegex = new(@"`([^`\r\n]+)`", RegexOptions.Compiled, RegexTimeout);

    // Emphasis & Strikethrough
    private static readonly Regex BoldItalicAsteriskRegex = new(@"\*\*\*(?!\s)([^\r\n*]+?)(?<!\s)\*\*\*", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex BoldItalicUnderscoreRegex = new(@"___(?!\s)([^\r\n_]+?)(?<!\s)___", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex BoldAsteriskRegex = new(@"\*\*(?!\s)([^\r\n*]+?)(?<!\s)\*\*", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex BoldUnderscoreRegex = new(@"__(?!\s)([^\r\n_]+?)(?<!\s)__", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex ItalicAsteriskRegex = new(@"(?<!\*)\*(?!\s|\*)([^\r\n*]+?)(?<!\s|\*)\*(?!\*)", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex ItalicUnderscoreRegex = new(@"(?<![a-zA-Z0-9_])_(?!\s|_)([^\r\n_]+?)(?<!\s|_)_(?![a-zA-Z0-9_])", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex StrikethroughRegex = new(@"~~(?!\s)([^\r\n~]+?)(?<!\s)~~", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex HighlightRegex = new(@"==(?!\s)([^\r\n=]+?)(?<!\s)==", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex FootnoteRefRegex = new(@"\[\^[\w\-]+\]", RegexOptions.Compiled, RegexTimeout);

    public (string Output, int Replacements) Apply(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (input, 0);
        }

        string[] tokens = LineSplitRegex.Split(input);
        var sb = new StringBuilder(input.Length);
        bool inCodeFence = false;
        int totalReplacements = 0;

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];

            // If token is a newline delimiter, write directly
            if (i % 2 == 1) // Delimiter token from Regex.Split
            {
                sb.Append(token);
                continue;
            }

            // Check if this line is a code fence toggle (```)
            if (token.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                sb.Append(token);
                continue;
            }

            if (inCodeFence)
            {
                // In code block: preserve verbatim
                sb.Append(token);
                continue;
            }

            var (cleanedLine, count) = StripMarkdownLine(token);
            totalReplacements += count;
            sb.Append(cleanedLine);
        }

        return (sb.ToString(), totalReplacements);
    }

    private static (string Output, int Replacements) StripMarkdownLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return (line, 0);
        }

        string current = line;
        int totalCount = 0;

        for (int iteration = 0; iteration < 5; iteration++)
        {
            int passCount = 0;

            void ApplyRegex(Regex regex, string replacement)
            {
                int matchCount = regex.Count(current);
                if (matchCount > 0)
                {
                    passCount += matchCount;
                    current = regex.Replace(current, replacement);
                }
            }

            // 1. Horizontal rules & table dividers
            ApplyRegex(HorizontalRuleRegex, string.Empty);
            ApplyRegex(TableDividerRegex, string.Empty);

            // 2. Structural line prefixes
            ApplyRegex(TaskListCheckboxRegex, string.Empty);
            ApplyRegex(ListBulletRegex, string.Empty);
            ApplyRegex(NumberedListRegex, string.Empty);
            ApplyRegex(HeaderRegex, string.Empty);
            ApplyRegex(TrailingHeaderHashesRegex, string.Empty);
            ApplyRegex(BlockquoteRegex, string.Empty);
            ApplyRegex(FootnoteRefRegex, string.Empty);

            // 3. Images & Links
            ApplyRegex(ImageRegex, "$1");
            ApplyRegex(LinkRegex, "$1");
            ApplyRegex(RefLinkRegex, "$1");
            ApplyRegex(AutoLinkRegex, "$1");

            // 4. Inline code
            ApplyRegex(InlineCodeRegex, "$1");

            // 5. Emphasis & Formatting
            ApplyRegex(BoldItalicAsteriskRegex, "$1");
            ApplyRegex(BoldItalicUnderscoreRegex, "$1");
            ApplyRegex(BoldAsteriskRegex, "$1");
            ApplyRegex(BoldUnderscoreRegex, "$1");
            ApplyRegex(ItalicAsteriskRegex, "$1");
            ApplyRegex(ItalicUnderscoreRegex, "$1");
            ApplyRegex(StrikethroughRegex, "$1");
            ApplyRegex(HighlightRegex, "$1");

            if (passCount == 0)
            {
                break;
            }

            totalCount += passCount;
        }

        return (current, totalCount);
    }
}
