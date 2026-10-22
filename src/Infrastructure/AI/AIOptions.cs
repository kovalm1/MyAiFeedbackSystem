namespace MyAiFeedbackSystem.Infrastructure.AI;

public enum ResponseFormatMode
{
    StrictJsonSchema,
    JsonObject,
    PromptOnly
}

public sealed class AIOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "Stub";
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 60;
    public ResponseFormatMode ResponseFormatMode { get; set; } = ResponseFormatMode.StrictJsonSchema;
}
