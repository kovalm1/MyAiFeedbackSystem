using MyAiFeedbackSystem.Core.Enums;

namespace MyAiFeedbackSystem.Core.Models;

public sealed record AIReviewAnalysisResult(
    ReviewSentiment Sentiment,
    ReviewPriority Priority,
    string Category,
    bool NeedUrgentResponse,
    string Summary,
    string ProviderName,
    string ModelName);
