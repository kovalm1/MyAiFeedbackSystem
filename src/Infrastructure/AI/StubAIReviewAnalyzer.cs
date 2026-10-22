using MyAiFeedbackSystem.Application.Abstractions;
using MyAiFeedbackSystem.Core.Enums;
using MyAiFeedbackSystem.Core.Models;

namespace MyAiFeedbackSystem.Infrastructure.AI;

public sealed class StubAIReviewAnalyzer : IAIReviewAnalyzer
{
    public Task<AIReviewAnalysisResult> AnalyzeAsync(
        string reviewText,
        CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled<AIReviewAnalysisResult>(ct);
        }

        var result = new AIReviewAnalysisResult(
            Sentiment: ReviewSentiment.Neutral,
            Priority: ReviewPriority.Normal,
            Category: "General",
            NeedUrgentResponse: false,
            Summary: "Демонстраційний результат: текст відгуку не аналізувався.",
            ProviderName: "Stub",
            ModelName: "Stub");

        return Task.FromResult(result);
    }
}
