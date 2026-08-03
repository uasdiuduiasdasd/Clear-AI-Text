namespace ClearAIText.Core.Models;

/// <summary>
/// Contains metrics and details for an executed transformation rule.
/// </summary>
public sealed record RuleExecutionDetail(
    string RuleName,
    RuleTier Tier,
    int ReplacementsCount);
