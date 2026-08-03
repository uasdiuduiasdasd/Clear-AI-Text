using ClearAIText.Core.Models;

namespace ClearAIText.Core.Pipeline;

/// <summary>
/// Core text processing pipeline contract.
/// </summary>
public interface ITextPipeline
{
    /// <summary>
    /// Executes normalization and sanitization on the input text using the specified profile.
    /// </summary>
    /// <param name="input">The raw input text from clipboard or sandbox.</param>
    /// <param name="profile">The active configuration profile.</param>
    /// <returns>A strongly-typed NormalizationResult containing cleaned text and execution metrics.</returns>
    NormalizationResult Process(string input, NormalizationProfile? profile = null);
}
