using MyAiFeedbackSystem.Core.Enums;

namespace MyAiFeedbackSystem.Core.Entities;

public sealed class Review
{
    private DateTimeOffset _createdAtUtc;
    private DateTimeOffset? _analyzedAtUtc;

    public Guid Id { get; set; }

    public required string AuthorName { get; set; }

    public required string AuthorEmail { get; set; }

    public required string Content { get; set; }

    public DateTimeOffset CreatedAtUtc
    {
        get => _createdAtUtc;
        set => _createdAtUtc = value.ToUniversalTime();
    }

    public ReviewSentiment? Sentiment { get; set; }

    public ReviewPriority? Priority { get; set; }

    public string? Category { get; set; }

    public bool? NeedUrgentResponse { get; set; }

    public string? Summary { get; set; }

    public string? DraftResponse { get; set; }

    public string? AIProviderName { get; set; }

    public string? AIModelName { get; set; }

    public DateTimeOffset? AnalyzedAtUtc
    {
        get => _analyzedAtUtc;
        set => _analyzedAtUtc = value?.ToUniversalTime();
    }
}
