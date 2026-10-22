namespace MyAiFeedbackSystem.WebApp.Client.SessionState;

public interface IClientSessionStateStore
{
    bool IsReady { get; }

    Task InitializeAsync(CancellationToken ct = default);
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);
    ValueTask SetAsync<T>(string key, T value, CancellationToken ct = default);
    ValueTask RemoveAsync(string key, CancellationToken ct = default);
}
