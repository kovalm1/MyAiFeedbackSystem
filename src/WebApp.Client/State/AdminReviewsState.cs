namespace MyAiFeedbackSystem.WebApp.Client.State;

public sealed class AdminReviewsState
{
    public Guid? SelectedReviewId { get; set; }
    public string Search { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}
