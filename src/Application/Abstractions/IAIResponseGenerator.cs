namespace MyAiFeedbackSystem.Application.Abstractions;

public interface IAIResponseGenerator
{
    Task<string> GenerateResponseAsync(
        string reviewText,
        string authorName,
        CancellationToken ct = default);
}
