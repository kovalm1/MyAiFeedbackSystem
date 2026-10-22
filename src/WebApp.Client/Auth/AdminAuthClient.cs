using System.Net.Http.Json;

namespace MyAiFeedbackSystem.WebApp.Client.Auth;

public sealed class AdminAuthClient(HttpClient http)
{
    public string? CsrfToken { get; private set; }

    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        var session = await http.GetFromJsonAsync<AdminSession>("api/auth/session", ct)
            ?? throw new HttpRequestException("Empty authentication session response.");
        CsrfToken = session.CsrfToken;
        return session.IsAuthenticated;
    }

    public HttpRequestMessage CreatePost(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-CSRF-TOKEN", CsrfToken
            ?? throw new InvalidOperationException("Authentication session was not initialized."));
        return request;
    }

    public sealed record AdminSession(bool IsAuthenticated, string? CsrfToken);
}
