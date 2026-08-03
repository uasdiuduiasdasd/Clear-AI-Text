using System.Diagnostics;
using ClearAIText.Core.Models;
using ClearAIText.Core.Rules;

namespace ClearAIText.Core.Pipeline;

/// <summary>
/// Default thread-safe implementation of the text transformation pipeline.
/// </summary>
public sealed class TextPipeline : ITextPipeline
{
    public const int MaxPayloadCharacterLimit = 5 * 1024 * 1024; // 5 MB

    private static readonly DashNormalizer StaticDashNormalizer = new();
    private static readonly QuoteNormalizer StaticQuoteNormalizer = new();
    private static readonly SpaceNormalizer StaticSpaceNormalizer = new();
    private static readonly DiacriticsStripper StaticDiacriticsStripper = new();
    private static readonly EmojiStripper StaticEmojiStripper = new();
    private static readonly MarkdownStripper StaticMarkdownStripper = new();
    private static readonly ControlCleaner StaticControlCleanerWithEllipses = new(true);
    private static readonly ControlCleaner StaticControlCleanerWithoutEllipses = new(false);

    private readonly Lock _customRulesLock = new();
    private List<CustomRule>? _lastCustomRulesSnapshot;
    private List<CustomRegexRule> _cachedCustomRules = [];

    public NormalizationResult Process(string input, NormalizationProfile? profile = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new NormalizationResult
            {
                OutputText = input ?? string.Empty,
                TotalReplacementsCount = 0,
                AppliedRules = [],
                ElapsedTime = TimeSpan.Zero,
                HasModifications = false,
                ConfusablesDetected = []
            };
        }

        // Payload size guard: Prevent OOM on enormous non-text or corrupt buffers
        if (input.Length > MaxPayloadCharacterLimit)
        {
            return new NormalizationResult
            {
                OutputText = input,
                TotalReplacementsCount = 0,
                AppliedRules = [],
                ElapsedTime = TimeSpan.Zero,
                HasModifications = false,
                ConfusablesDetected = []
            };
        }

        profile ??= NormalizationProfile.CreateSafeDefault();
        if (!profile.IsEnabled)
        {
            return new NormalizationResult
            {
                OutputText = input,
                TotalReplacementsCount = 0,
                AppliedRules = [],
                ElapsedTime = TimeSpan.Zero,
                HasModifications = false,
                ConfusablesDetected = []
            };
        }

        var stopwatch = Stopwatch.StartNew();

        string currentText = input;
        int totalReplacements = 0;
        var appliedRules = new List<RuleExecutionDetail>();

        // 1. Tier 1: Safe Normalization
        if (profile.CleanInvisibleControls || profile.NormalizeEllipses)
        {
            var cleaner = profile.NormalizeEllipses ? StaticControlCleanerWithEllipses : StaticControlCleanerWithoutEllipses;
            var (output, count) = cleaner.Apply(currentText);
            if (count > 0)
            {
                currentText = output;
                totalReplacements += count;
                appliedRules.Add(new RuleExecutionDetail(cleaner.Name, cleaner.Tier, count));
            }
        }

        if (profile.ReplaceDashes)
        {
            var (output, count) = StaticDashNormalizer.Apply(currentText);
            if (count > 0)
            {
                currentText = output;
                totalReplacements += count;
                appliedRules.Add(new RuleExecutionDetail(StaticDashNormalizer.Name, StaticDashNormalizer.Tier, count));
            }
        }

        if (profile.ReplaceQuotes)
        {
            var (output, count) = StaticQuoteNormalizer.Apply(currentText);
            if (count > 0)
            {
                currentText = output;
                totalReplacements += count;
                appliedRules.Add(new RuleExecutionDetail(StaticQuoteNormalizer.Name, StaticQuoteNormalizer.Tier, count));
            }
        }

        if (profile.NormalizeSpaces)
        {
            var (output, count) = StaticSpaceNormalizer.Apply(currentText);
            if (count > 0)
            {
                currentText = output;
                totalReplacements += count;
                appliedRules.Add(new RuleExecutionDetail(StaticSpaceNormalizer.Name, StaticSpaceNormalizer.Tier, count));
            }
        }

        // 2. Tier 2: Destructive Transformations
        if (profile.StripDiacritics)
        {
            var (output, count) = StaticDiacriticsStripper.Apply(currentText);
            if (count > 0)
            {
                currentText = output;
                totalReplacements += count;
                appliedRules.Add(new RuleExecutionDetail(StaticDiacriticsStripper.Name, StaticDiacriticsStripper.Tier, count));
            }
        }

        if (profile.StripEmojis)
        {
            var (output, count) = StaticEmojiStripper.Apply(currentText);
            if (count > 0)
            {
                currentText = output;
                totalReplacements += count;
                appliedRules.Add(new RuleExecutionDetail(StaticEmojiStripper.Name, StaticEmojiStripper.Tier, count));
            }
        }

        if (profile.StripLightMarkdown)
        {
            var (output, count) = StaticMarkdownStripper.Apply(currentText);
            if (count > 0)
            {
                currentText = output;
                totalReplacements += count;
                appliedRules.Add(new RuleExecutionDetail(StaticMarkdownStripper.Name, StaticMarkdownStripper.Tier, count));
            }
        }

        // 3. User Custom Rules (Cached)
        if (profile.CustomRules is { Count: > 0 })
        {
            var rulesToRun = GetCachedCustomRules(profile.CustomRules);
            foreach (var customRule in rulesToRun)
            {
                var (output, count) = customRule.Apply(currentText);
                if (count > 0)
                {
                    currentText = output;
                    totalReplacements += count;
                    appliedRules.Add(new RuleExecutionDetail(customRule.Name, customRule.Tier, count));
                }
            }
        }

        // 4. Tier 3: Heuristic Confusables Detection
        IReadOnlyList<string> confusables = [];
        if (profile.DetectConfusables)
        {
            confusables = HomoglyphDetector.Analyze(currentText);
        }

        stopwatch.Stop();

        bool hasModifications = totalReplacements > 0 && !string.Equals(input, currentText, StringComparison.Ordinal);

        return new NormalizationResult
        {
            OutputText = currentText,
            TotalReplacementsCount = totalReplacements,
            AppliedRules = appliedRules,
            ElapsedTime = stopwatch.Elapsed,
            HasModifications = hasModifications,
            ConfusablesDetected = confusables
        };
    }

    private List<CustomRegexRule> GetCachedCustomRules(List<CustomRule> customRules)
    {
        lock (_customRulesLock)
        {
            if (_lastCustomRulesSnapshot != null && AreCustomRulesEqual(_lastCustomRulesSnapshot, customRules))
            {
                return _cachedCustomRules;
            }

            _lastCustomRulesSnapshot = customRules.Select(r => r with { }).ToList();
            _cachedCustomRules = customRules
                .Where(r => r.IsEnabled)
                .Select(r => new CustomRegexRule(r))
                .ToList();

            return _cachedCustomRules;
        }
    }

    private static bool AreCustomRulesEqual(List<CustomRule> a, List<CustomRule> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            var rA = a[i];
            var rB = b[i];
            if (rA.IsEnabled != rB.IsEnabled ||
                rA.IsRegex != rB.IsRegex ||
                !string.Equals(rA.FindPattern, rB.FindPattern, StringComparison.Ordinal) ||
                !string.Equals(rA.Replacement, rB.Replacement, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}

