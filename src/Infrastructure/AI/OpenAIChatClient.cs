using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MyAiFeedbackSystem.Core.Entities;
using MyAiFeedbackSystem.Core.Enums;
using MyAiFeedbackSystem.Core.Models;

namespace MyAiFeedbackSystem.Infrastructure.AI;

internal sealed class OpenAIChatClient
{
    private const string AnalysisPromptResource = "MyAiFeedbackSystem.Infrastructure.Prompts.analysis_prompt.md";
    private const string ResponsePromptResource = "MyAiFeedbackSystem.Infrastructure.Prompts.response_prompt.md";

    private static readonly JsonElement AnalysisSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": {
            "Sentiment": { "type": "string", "enum": ["Positive", "Neutral", "Negative", "Mixed"] },
            "Priority": { "type": "string", "enum": ["Low", "Normal", "High", "Critical"] },
            "Category": { "type": "string" },
            "NeedUrgentResponse": { "type": "boolean" },
            "Summary": { "type": "string" },
            "ProviderName": { "type": "string" },
            "ModelName": { "type": "string" }
          },
          "required": ["Sentiment", "Priority", "Category", "NeedUrgentResponse", "Summary", "ProviderName", "ModelName"],
          "additionalProperties": false
        }
        """);

    private static readonly JsonElement ResponseSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": { "draftResponse": { "type": "string" } },
          "required": ["draftResponse"],
          "additionalProperties": false
        }
        """);

    private readonly HttpClient _httpClient;
    private readonly AIOptions _options;
    private readonly Uri _baseUrl;

    public OpenAIChatClient(HttpClient httpClient, AIOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Provider is not ("OpenAI" or "LMStudio" or "Ollama"))
            throw new ArgumentException("AI:Provider must be OpenAI, LMStudio, or Ollama for the HTTP client.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.BaseUrl)
            || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl)
            || baseUrl.Scheme is not ("http" or "https")
            || !baseUrl.AbsolutePath.EndsWith('/')
            || !string.IsNullOrEmpty(baseUrl.Query)
            || !string.IsNullOrEmpty(baseUrl.Fragment))
            throw new ArgumentException("AI:BaseUrl must be an absolute HTTP(S) API URL ending in '/'.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new ArgumentException("AI:Model is required for an HTTP AI provider.", nameof(options));
        if (options.Model.Length > ReviewFieldLimits.AIModelName)
            throw new ArgumentException("AI:Model exceeds the supported length.", nameof(options));
        if (options.RequestTimeoutSeconds is < 1 or > 300)
            throw new ArgumentException("AI:RequestTimeoutSeconds must be between 1 and 300.", nameof(options));
        if (!Enum.IsDefined(options.ResponseFormatMode))
            throw new ArgumentException("AI:ResponseFormatMode is invalid.", nameof(options));
        if (options.Provider == "OpenAI" && string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("AI:ApiKey is required for OpenAI.", nameof(options));
        if (options.Provider == "OpenAI"
            && baseUrl != new Uri("https://api.openai.com/v1/"))
            throw new ArgumentException("AI:BaseUrl must be https://api.openai.com/v1/ for OpenAI.", nameof(options));

        _httpClient = httpClient;
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        _options = options;
        _baseUrl = baseUrl;
    }

    public async Task<AIReviewAnalysisResult> AnalyzeAsync(string reviewText, CancellationToken ct)
    {
        var userData = JsonSerializer.Serialize(new
        {
            reviewText,
            providerName = _options.Provider,
            modelName = _options.Model
        });
        var content = await CompleteAsync(
            ReadPrompt(AnalysisPromptResource), userData, AnalysisSchema, "review_analysis", ct);

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var sentiment = GetEnum<ReviewSentiment>(root, "Sentiment");
            var priority = GetEnum<ReviewPriority>(root, "Priority");
            var category = GetString(root, "Category", ReviewFieldLimits.Category);
            var urgent = GetBoolean(root, "NeedUrgentResponse");
            var summary = GetString(root, "Summary", ReviewFieldLimits.Summary);
            _ = GetString(root, "ProviderName", ReviewFieldLimits.AIProviderName);
            _ = GetString(root, "ModelName", ReviewFieldLimits.AIModelName);

            return new AIReviewAnalysisResult(
                sentiment, priority, category, urgent, summary, _options.Provider, _options.Model!);
        }
        catch (JsonException ex)
        {
            throw new AIProviderException("AI analysis returned invalid or incomplete JSON.", ex);
        }
    }

    public async Task<string> GenerateResponseAsync(
        string reviewText, string authorName, CancellationToken ct)
    {
        var prompt = ReadPrompt(ResponsePromptResource);
        if (_options.ResponseFormatMode == ResponseFormatMode.PromptOnly)
            prompt += "\nReturn only the draft reply as plain text.";
        else
            prompt += "\nReturn a JSON object with one field named draftResponse containing the draft reply.";

        var userData = JsonSerializer.Serialize(new { reviewText, authorName });
        var content = await CompleteAsync(prompt, userData, ResponseSchema, "draft_response", ct);

        if (_options.ResponseFormatMode == ResponseFormatMode.PromptOnly)
            return ValidateDraft(content);

        try
        {
            using var document = JsonDocument.Parse(content);
            return ValidateDraft(GetString(document.RootElement, "draftResponse", ReviewFieldLimits.DraftResponse));
        }
        catch (JsonException ex)
        {
            throw new AIProviderException("AI response returned invalid or incomplete JSON.", ex);
        }
    }

    private async Task<string> CompleteAsync(
        string systemPrompt, string userData, JsonElement schema, string schemaName, CancellationToken ct)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userData }
            }
        };

        if (_options.ResponseFormatMode == ResponseFormatMode.StrictJsonSchema)
        {
            payload["response_format"] = new
            {
                type = "json_schema",
                json_schema = new { name = schemaName, strict = true, schema }
            };
        }
        else if (_options.ResponseFormatMode == ResponseFormatMode.JsonObject)
        {
            payload["response_format"] = new { type = "json_object" };
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post, new Uri(_baseUrl, "chat/completions"))
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

        try
        {
            using var response = await _httpClient.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new AIProviderException($"AI provider returned HTTP {(int)response.StatusCode}.");

            var responseText = await response.Content.ReadAsStringAsync(timeout.Token);
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
                throw new AIProviderException("AI provider returned no completion choice.");

            var choice = choices[0];
            if (choice.ValueKind != JsonValueKind.Object)
                throw new AIProviderException("AI provider returned an invalid completion choice.");
            if (choice.TryGetProperty("finish_reason", out var finishReason)
                && finishReason.ValueKind == JsonValueKind.String
                && finishReason.GetString() == "length")
                throw new AIProviderException("AI provider truncated the completion.");
            if (!choice.TryGetProperty("message", out var message)
                || message.ValueKind != JsonValueKind.Object
                || !message.TryGetProperty("content", out var responseContent)
                || responseContent.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(responseContent.GetString()))
                throw new AIProviderException("AI provider returned empty completion content.");

            return responseContent.GetString()!;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new AIProviderException("AI provider request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new AIProviderException("AI provider could not be reached.", ex);
        }
        catch (JsonException ex)
        {
            throw new AIProviderException("AI provider returned an invalid completion envelope.", ex);
        }
    }

    private static string ReadPrompt(string resourceName)
    {
        using var stream = typeof(OpenAIChatClient).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded AI prompt '{resourceName}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string GetString(JsonElement root, string name, int maxLength)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString())
            || property.GetString()!.Trim().Length > maxLength)
            throw new JsonException($"Missing or invalid AI field: {name}.");
        return property.GetString()!.Trim();
    }

    private static bool GetBoolean(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(name, out var property)
            || property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new JsonException($"Missing or invalid AI field: {name}.");
        return property.GetBoolean();
    }

    private static T GetEnum<T>(JsonElement root, string name) where T : struct, Enum
    {
        var value = GetString(root, name, 100);
        if (!Enum.TryParse<T>(value, out var parsed) || Enum.GetName(parsed) != value)
            throw new JsonException($"Invalid AI field value: {name}.");
        return parsed;
    }

    private static string ValidateDraft(string draft)
    {
        var trimmed = draft.Trim();
        if (trimmed.Length is 0 or > ReviewFieldLimits.DraftResponse)
            throw new AIProviderException("AI provider returned an empty or oversized draft response.");
        return trimmed;
    }
}
