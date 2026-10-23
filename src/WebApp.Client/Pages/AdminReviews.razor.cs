using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MyAiFeedbackSystem.WebApp.Client.Auth;
using MyAiFeedbackSystem.WebApp.Client.Contracts;
using MyAiFeedbackSystem.WebApp.Client.SessionState;
using MyAiFeedbackSystem.WebApp.Client.State;

namespace MyAiFeedbackSystem.WebApp.Client.Pages;

public partial class AdminReviews : IDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IClientSessionStateStore StateStore { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AdminAuthClient Auth { get; set; } = default!;

    private readonly CancellationTokenSource _disposeToken = new();
    private readonly SemaphoreSlim _storageLock = new(1, 1);
    private List<AdminReviewItem> _reviews = [];
    private AdminReviewsState _state = new();
    private AdminReviewDraft _draft = new();
    private bool _ready;
    private bool _storageError;
    private bool _loading;
    private bool _loadError;
    private bool _authenticated;
    private readonly HashSet<Guid> _analyzingIds = [];
    private readonly HashSet<Guid> _generatingIds = [];
    private int _stateVersion;
    private int _draftVersion;

    private AdminReviewItem? SelectedReview =>
        _reviews.FirstOrDefault(review => review.Id == _state.SelectedReviewId);

    private IEnumerable<AdminReviewItem> FilteredReviews => _reviews.Where(review =>
        (string.IsNullOrWhiteSpace(_state.Search)
            || review.AuthorName.Contains(_state.Search, StringComparison.OrdinalIgnoreCase)
            || review.Content.Contains(_state.Search, StringComparison.OrdinalIgnoreCase))
        && (string.IsNullOrEmpty(_state.Sentiment)
            || (_state.Sentiment == "Unanalyzed" ? review.Sentiment is null : review.Sentiment == _state.Sentiment))
        && (string.IsNullOrEmpty(_state.Priority)
            || (_state.Priority == "Unanalyzed" ? review.Priority is null : review.Priority == _state.Priority)));

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _authenticated = await Auth.RefreshAsync(_disposeToken.Token);
            if (!_authenticated)
            {
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            await StateStore.InitializeAsync(_disposeToken.Token);
            _state = await StateStore.GetAsync<AdminReviewsState>(SessionStateKeys.AdminReviewsState,
                _disposeToken.Token) ?? new AdminReviewsState();
            if (_state.SelectedReviewId is { } id)
                _draft = await StateStore.GetAsync<AdminReviewDraft>(SessionStateKeys.AdminReviewDraft(id),
                    _disposeToken.Token) ?? new AdminReviewDraft();
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or System.Text.Json.JsonException)
        {
            _storageError = true;
        }
        catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
        {
            return;
        }
        catch (HttpRequestException)
        {
            _loadError = true;
        }

        _ready = true;
        await InvokeAsync(StateHasChanged);
        await LoadReviewsAsync();
    }

    private async Task LoadReviewsAsync()
    {
        if (_loading || !_authenticated)
            return;

        _loading = true;
        _loadError = false;
        try
        {
            using var response = await Http.GetAsync("api/admin/reviews", _disposeToken.Token);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }
            response.EnsureSuccessStatusCode();
            _reviews = await response.Content.ReadFromJsonAsync<List<AdminReviewItem>>(_disposeToken.Token) ?? [];
            if (_state.SelectedReviewId is not null && SelectedReview is null)
            {
                _state.SelectedReviewId = null;
                _draft = new AdminReviewDraft();
                await SaveStateAsync();
            }
        }
        catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
        {
        }
        catch (TaskCanceledException)
        {
            _loadError = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            _loadError = true;
        }
        finally
        {
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SelectReviewAsync(Guid id)
    {
        if (_state.SelectedReviewId == id)
            return;

        _state.SelectedReviewId = id;
        _draft = new AdminReviewDraft();
        var version = ++_draftVersion;
        await SaveStateAsync();
        if (!_storageError)
        {
            try
            {
                var draft = await StateStore.GetAsync<AdminReviewDraft>(SessionStateKeys.AdminReviewDraft(id),
                    _disposeToken.Token);
                if (_state.SelectedReviewId == id && version == _draftVersion)
                    _draft = draft ?? new AdminReviewDraft();
            }
            catch (Exception ex) when (ex is JSException or InvalidOperationException or System.Text.Json.JsonException)
            {
                _storageError = true;
            }
        }
    }

    private async Task OnSentimentChanged(string value)
    {
        _state.Sentiment = value;
        await SaveStateAsync();
    }

    private async Task OnPriorityChanged(string value)
    {
        _state.Priority = value;
        await SaveStateAsync();
    }

    private async Task SaveStateAsync()
    {
        if (!_ready || _storageError)
            return;

        var version = ++_stateVersion;
        var snapshot = new AdminReviewsState
        {
            SelectedReviewId = _state.SelectedReviewId,
            Search = _state.Search,
            Sentiment = _state.Sentiment,
            Priority = _state.Priority
        };
        await SaveStorageAsync(SessionStateKeys.AdminReviewsState, snapshot, () => version == _stateVersion);
    }

    private Task SaveDraftAsync()
    {
        if (_state.SelectedReviewId is not { } id || !_ready || _storageError)
            return Task.CompletedTask;

        var version = ++_draftVersion;
        return SaveStorageAsync(SessionStateKeys.AdminReviewDraft(id),
            new AdminReviewDraft { Text = _draft.Text },
            () => version == _draftVersion && _state.SelectedReviewId == id);
    }

    private async Task SaveStorageAsync<T>(string key, T value, Func<bool> isCurrent)
    {
        try
        {
            await _storageLock.WaitAsync(_disposeToken.Token);
            try
            {
                if (isCurrent())
                    await StateStore.SetAsync(key, value, _disposeToken.Token);
            }
            finally
            {
                _storageLock.Release();
            }
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            _storageError = true;
            Snackbar.Add("Не вдалося зберегти стан вкладки.", Severity.Warning);
        }
        catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
        {
        }
    }

    private async Task AnalyzeAsync(Guid id)
    {
        if (!_analyzingIds.Add(id))
            return;

        try
        {
            using var request = Auth.CreatePost($"api/admin/reviews/{id:D}/analyze");
            using var response = await Http.SendAsync(request, _disposeToken.Token);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }
            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Не вдалося виконати аналіз. Поточний відгук і фільтри збережено.", Severity.Error);
                return;
            }

            var updated = await response.Content.ReadFromJsonAsync<AdminReviewItem>(_disposeToken.Token);
            if (updated is not null)
            {
                var index = _reviews.FindIndex(review => review.Id == id);
                if (index >= 0)
                    _reviews[index] = updated;
            }
            Snackbar.Add("Аналіз збережено.", Severity.Success);
        }
        catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
        {
        }
        catch (TaskCanceledException)
        {
            Snackbar.Add("Час очікування аналізу вичерпано. Спробуйте ще раз.", Severity.Error);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            Snackbar.Add("З’єднання з сервером недоступне. Спробуйте ще раз.", Severity.Error);
        }
        finally
        {
            _analyzingIds.Remove(id);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task GenerateDraftAsync(Guid id)
    {
        if (!_generatingIds.Add(id))
            return;

        try
        {
            using var request = Auth.CreatePost($"api/admin/reviews/{id:D}/draft");
            using var response = await Http.SendAsync(request, _disposeToken.Token);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }
            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Не вдалося згенерувати чернетку. Поточний текст збережено.", Severity.Error);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<GeneratedReviewDraft>(_disposeToken.Token);
            if (result is null)
                throw new System.Text.Json.JsonException("Empty draft response.");

            if (_state.SelectedReviewId == id)
            {
                _draft.Text = result.Text;
                await SaveDraftAsync();
            }
            Snackbar.Add("AI-чернетку створено. Перевірте її перед використанням.", Severity.Success);
        }
        catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
        {
        }
        catch (TaskCanceledException)
        {
            Snackbar.Add("Час очікування чернетки вичерпано. Поточний текст збережено.", Severity.Error);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            Snackbar.Add("З’єднання з сервером недоступне. Поточний текст збережено.", Severity.Error);
        }
        finally
        {
            _generatingIds.Remove(id);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LogoutAsync()
    {
        if (!_authenticated)
            return;

        try
        {
            using var request = Auth.CreatePost("api/auth/logout");
            using var response = await Http.SendAsync(request, _disposeToken.Token);
            if (response.IsSuccessStatusCode)
                Navigation.NavigateTo("/login", forceLoad: true);
            else
                Snackbar.Add("Не вдалося вийти. Спробуйте ще раз.", Severity.Error);
        }
        catch (HttpRequestException)
        {
            Snackbar.Add("Не вдалося вийти. Спробуйте ще раз.", Severity.Error);
        }
    }

    public void Dispose()
    {
        _disposeToken.Cancel();
        _disposeToken.Dispose();
    }
}
