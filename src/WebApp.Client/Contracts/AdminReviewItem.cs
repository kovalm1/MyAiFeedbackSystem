namespace MyAiFeedbackSystem.WebApp.Client.Contracts;

public sealed record AdminReviewItem(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    string AuthorName,
    string Content,
    string? Sentiment,
    string? Priority,
    string? Category,
    bool? NeedUrgentResponse,
    string? Summary,
    string? AIProviderName,
    string? AIModelName,
    DateTimeOffset? AnalyzedAtUtc);
