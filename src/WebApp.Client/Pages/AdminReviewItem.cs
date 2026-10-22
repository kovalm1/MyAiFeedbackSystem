namespace MyAiFeedbackSystem.WebApp.Client.Pages;

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

public sealed record GeneratedReviewDraft(string Text);
