using System.Globalization;
using ClearAIText.Core.Models;

namespace ClearAIText.App.Models;

/// <summary>
/// Represents an entry in the clipboard processing history.
/// </summary>
public sealed class HistoryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string OriginalText { get; set; } = string.Empty;
    public string CleanedText { get; set; } = string.Empty;
    public string SourceProcess { get; set; } = "Неизвестно";
    public IReadOnlyList<RuleExecutionDetail> AppliedRules { get; set; } = [];

    public int ReplacementsCount => AppliedRules.Sum(r => r.ReplacementsCount);
    public int CharsSavedCount => Math.Max(0, OriginalText.Length - CleanedText.Length);
    public string FormattedTime => Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    public string SummaryText
    {
        get
        {
            if (AppliedRules.Count == 0)
            {
                return "Изменений нет";
            }

            var groupCounts = AppliedRules
                .Where(r => r.ReplacementsCount > 0)
                .Select(r => $"{r.RuleName}: {r.ReplacementsCount}");

            return string.Join(", ", groupCounts);
        }
    }
}
