namespace MyAiFeedbackSystem.Core.Entities;

public static class ReviewFieldLimits
{
    public const int AuthorName = 200;
    public const int AuthorEmail = 320;
    public const int Category = 100;
    public const int Summary = 2_000;
    public const int DraftResponse = 10_000;
    public const int AIProviderName = 100;
    public const int AIModelName = 200;
}
