using MyAiFeedbackSystem.Infrastructure.AI;
using MyAiFeedbackSystem.Core.Entities;

namespace MyAiFeedbackSystem.WebApp.AI;

internal static class AIConfiguration
{
    public static void Validate(AIOptions options)
    {
        if (options.Provider == "Stub")
            return;

        if (options.Provider is not ("LMStudio" or "Ollama" or "OpenAI"))
            throw new InvalidOperationException(
                $"Unknown AI:Provider '{options.Provider}'. Use Stub, LMStudio, Ollama, or OpenAI.");

        if (string.IsNullOrWhiteSpace(options.BaseUrl)
            || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl)
            || baseUrl.Scheme is not ("http" or "https")
            || !baseUrl.AbsolutePath.EndsWith('/')
            || !string.IsNullOrEmpty(baseUrl.Query)
            || !string.IsNullOrEmpty(baseUrl.Fragment))
            throw new InvalidOperationException("AI:BaseUrl must be an absolute HTTP(S) API URL ending in '/'.");

        if (string.IsNullOrWhiteSpace(options.Model))
            throw new InvalidOperationException("AI:Model is required for the selected AI provider.");
        if (options.Model.Length > ReviewFieldLimits.AIModelName)
            throw new InvalidOperationException("AI:Model exceeds the supported length.");
        if (options.RequestTimeoutSeconds is < 1 or > 300)
            throw new InvalidOperationException("AI:RequestTimeoutSeconds must be between 1 and 300.");
        if (!Enum.IsDefined(options.ResponseFormatMode))
            throw new InvalidOperationException("AI:ResponseFormatMode is invalid.");

        if (options.Provider == "OpenAI")
        {
            if (baseUrl != new Uri("https://api.openai.com/v1/"))
                throw new InvalidOperationException("OpenAI requires AI:BaseUrl = https://api.openai.com/v1/.");
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new InvalidOperationException("OpenAI requires AI:ApiKey from User Secrets or an environment variable.");
        }
    }
}
