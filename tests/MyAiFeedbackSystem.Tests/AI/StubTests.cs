using MyAiFeedbackSystem.Core.Enums;
using MyAiFeedbackSystem.Infrastructure.AI;

namespace MyAiFeedbackSystem.Tests;

public sealed class StubTests
{
    [Fact]
    public async Task Analyzer_returns_deterministic_demo_result()
    {
        var analyzer = new StubAIReviewAnalyzer();
        var first = await analyzer.AnalyzeAsync("One");
        var second = await analyzer.AnalyzeAsync("Two");
        Assert.Equal(first, second);
        Assert.Equal(ReviewSentiment.Neutral, first.Sentiment);
        Assert.Equal(ReviewPriority.Normal, first.Priority);
        Assert.Equal("Stub", first.ProviderName);
    }

    [Fact]
    public async Task Generator_uses_name_and_respects_cancellation()
    {
        var generator = new StubAIResponseGenerator();
        Assert.Contains("Olena", await generator.GenerateResponseAsync("Text", " Olena "));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            generator.GenerateResponseAsync("Text", "Olena", canceled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new StubAIReviewAnalyzer().AnalyzeAsync("Text", canceled.Token));
    }
}
