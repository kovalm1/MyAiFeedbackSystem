namespace MyAiFeedbackSystem.Infrastructure.AI;

public sealed class AIProviderException(string message, Exception? innerException = null)
    : Exception(message, innerException);
