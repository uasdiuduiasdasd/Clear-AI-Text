using System.Text.RegularExpressions;
using ClearAIText.Core.Models;

namespace ClearAIText.Core.Rules;

/// <summary>
/// User-defined rule supporting literal string or non-backtracking regular expression replacement.
/// </summary>
public sealed class CustomRegexRule : IRule
{
    public string Name { get; }
    public RuleTier Tier => RuleTier.Custom;

    private readonly string _replacement;
    private readonly Regex? _regex;
    private readonly string? _literalFind;

    public CustomRegexRule(CustomRule customRule)
    {
        Name = string.IsNullOrWhiteSpace(customRule.Name) ? "Custom Rule" : customRule.Name;
        _replacement = customRule.Replacement;

        if (customRule.IsRegex)
        {
            try
            {
                _regex = new Regex(
                    customRule.FindPattern,
                    RegexOptions.NonBacktracking | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(100));
            }
            catch (ArgumentException)
            {
                // Fallback to literal search if regex pattern is invalid or unsupported by non-backtracking
                _literalFind = customRule.FindPattern;
            }
        }
        else
        {
            _literalFind = customRule.FindPattern;
        }
    }

    public (string Output, int Replacements) Apply(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (input, 0);
        }

        if (_regex != null)
        {
            try
            {
                int matches = _regex.Count(input);
                if (matches == 0)
                {
                    return (input, 0);
                }

                string result = _regex.Replace(input, _replacement);
                return (result, matches);
            }
            catch (RegexMatchTimeoutException)
            {
                return (input, 0);
            }
        }

        if (!string.IsNullOrEmpty(_literalFind))
        {
            int occurrences = CountOccurrences(input, _literalFind);
            if (occurrences == 0)
            {
                return (input, 0);
            }

            string result = input.Replace(_literalFind, _replacement, StringComparison.Ordinal);
            return (result, occurrences);
        }

        return (input, 0);
    }

    private static int CountOccurrences(string source, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }
}
