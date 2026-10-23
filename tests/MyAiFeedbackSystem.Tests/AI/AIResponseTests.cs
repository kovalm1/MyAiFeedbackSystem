using System.Net;
using System.Text.Json;
using MyAiFeedbackSystem.Infrastructure.AI;

namespace MyAiFeedbackSystem.Tests;

public sealed class AIResponseTests
{
    [Fact]
    public async Task Valid_analysis_response_is_parsed()
    {
        var analyzer = NewAnalyzer(HttpStatusCode.OK, """
            {"Sentiment":"Positive","Priority":"Low","Category":"Service","NeedUrgentResponse":false,
             "Summary":"Helpful staff","ProviderName":"LMStudio","ModelName":"test-model"}
            """);
        var result = await analyzer.AnalyzeAsync("Helpful staff");
        Assert.Equal("Service", result.Category);
        Assert.Equal("LMStudio", result.ProviderName);
        Assert.Equal("test-model", result.ModelName);
    }

    [Theory]
    [InlineData("{\"Sentiment\":\"Positive\"}")]
    [InlineData("not json")]
    public async Task Incomplete_or_invalid_analysis_is_rejected(string content)
    {
        var analyzer = NewAnalyzer(HttpStatusCode.OK, content);
        await Assert.ThrowsAsync<AIProviderException>(() => analyzer.AnalyzeAsync("Text"));
    }

    [Fact]
    public async Task Provider_error_is_rejected_without_returning_response_body()
    {
        var analyzer = NewAnalyzer(HttpStatusCode.BadGateway, "secret response body");
        var error = await Assert.ThrowsAsync<AIProviderException>(() => analyzer.AnalyzeAsync("Text"));
        Assert.DoesNotContain("secret response body", error.Message);
    }

    private static OpenAIReviewAnalyzer NewAnalyzer(HttpStatusCode status, string content)
    {
        var handler = new FakeHandler(() => new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { content } } }
            }))
        });
        var options = new AIOptions
        {
            Provider = "LMStudio", BaseUrl = "http://localhost:1234/v1/", Model = "test-model",
            ResponseFormatMode = ResponseFormatMode.JsonObject
        };
        return new OpenAIReviewAnalyzer(new HttpClient(handler), options);
    }

    private sealed class FakeHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond());
    }
}
