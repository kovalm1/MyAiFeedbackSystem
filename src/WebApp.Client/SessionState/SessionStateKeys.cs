namespace MyAiFeedbackSystem.WebApp.Client.SessionState;

public static class SessionStateKeys
{
    public const string SubmitReviewDraft = "my-ai-feedback:submit-review-draft:v1";
    public const string AdminReviewsState = "my-ai-feedback:admin-reviews-state:v1";

    public static string AdminReviewDraft(Guid reviewId) =>
        $"my-ai-feedback:admin-review-draft:v1:{reviewId:D}";
}
