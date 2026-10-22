using MyAiFeedbackSystem.Application.Abstractions;
using MyAiFeedbackSystem.Core.Models;

namespace MyAiFeedbackSystem.Infrastructure.AI;

public sealed class OpenAIReviewAnalyzer : IAIReviewAnalyzer
{
    private readonly OpenAIChatClient _client;

    public OpenAIReviewAnalyzer(HttpClient httpClient, AIOptions options)
    {
        _client = new OpenAIChatClient(httpClient, options);
    }

    public Task<AIReviewAnalysisResult> AnalyzeAsync(
        string reviewText, CancellationToken ct = default)
        => _client.AnalyzeAsync(reviewText, ct);
}
