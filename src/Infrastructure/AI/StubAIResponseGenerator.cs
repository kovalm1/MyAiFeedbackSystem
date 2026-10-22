using MyAiFeedbackSystem.Application.Abstractions;

namespace MyAiFeedbackSystem.Infrastructure.AI;

public sealed class StubAIResponseGenerator : IAIResponseGenerator
{
    public Task<string> GenerateResponseAsync(
        string reviewText,
        string authorName,
        CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(ct);
        }

        var greeting = string.IsNullOrWhiteSpace(authorName)
            ? "Вітаємо!"
            : $"Вітаємо, {authorName.Trim()}!";

        return Task.FromResult(
            $"Демонстраційна чернетка. {greeting} Дякуємо за ваш відгук.");
    }
}
