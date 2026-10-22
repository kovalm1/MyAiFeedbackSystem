using MyAiFeedbackSystem.Application.Abstractions;

namespace MyAiFeedbackSystem.Infrastructure.AI;

public sealed class OpenAIResponseGenerator : IAIResponseGenerator
{
    private readonly OpenAIChatClient _client;

    public OpenAIResponseGenerator(HttpClient httpClient, AIOptions options)
    {
        _client = new OpenAIChatClient(httpClient, options);
    }

    public Task<string> GenerateResponseAsync(
        string reviewText, string authorName, CancellationToken ct = default)
        => _client.GenerateResponseAsync(reviewText, authorName, ct);
}
