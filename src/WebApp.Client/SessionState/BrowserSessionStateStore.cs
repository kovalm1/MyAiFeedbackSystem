using System.Text.Json;
using Microsoft.JSInterop;

namespace MyAiFeedbackSystem.WebApp.Client.SessionState;

public sealed class BrowserSessionStateStore(IJSRuntime jsRuntime) : IClientSessionStateStore
{
    private const string ProbeKey = "my-ai-feedback:storage-probe:v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool IsReady { get; private set; }

    // Call from OnAfterRenderAsync; JS interop is unavailable during prerendering.
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (IsReady)
            return;

        await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", ct, ProbeKey, "ready");
        string? value;
        try
        {
            value = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", ct, ProbeKey);
        }
        finally
        {
            await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", ct, ProbeKey);
        }

        if (value != "ready")
            throw new InvalidOperationException("Browser session storage is unavailable.");
        IsReady = true;
    }

    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        EnsureReady(key);
        var json = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", ct, key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public ValueTask SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        EnsureReady(key);
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return jsRuntime.InvokeVoidAsync("sessionStorage.setItem", ct, key, json);
    }

    public ValueTask RemoveAsync(string key, CancellationToken ct = default)
    {
        EnsureReady(key);
        return jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", ct, key);
    }

    private void EnsureReady(string key)
    {
        if (!IsReady)
            throw new InvalidOperationException("Session storage must be initialized after interactive rendering.");
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A session storage key is required.", nameof(key));
    }
}
