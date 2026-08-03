namespace ClearAIText.Core.Models;

/// <summary>
/// Represents the outcome of running a text payload through the transformation pipeline.
/// </summary>
public sealed record NormalizationResult
{
    public required string OutputText { get; init; }
    public required int TotalReplacementsCount { get; init; }
    public required IReadOnlyList<RuleExecutionDetail> AppliedRules { get; init; }
    public required TimeSpan ElapsedTime { get; init; }
    public required bool HasModifications { get; init; }
    public IReadOnlyList<string> ConfusablesDetected { get; init; } = [];
}
