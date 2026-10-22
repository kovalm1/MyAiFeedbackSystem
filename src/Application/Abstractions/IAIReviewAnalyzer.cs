using MyAiFeedbackSystem.Core.Models;

namespace MyAiFeedbackSystem.Application.Abstractions;

public interface IAIReviewAnalyzer
{
    Task<AIReviewAnalysisResult> AnalyzeAsync(
        string reviewText,
        CancellationToken ct = default);
}
