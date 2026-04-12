using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    string? AccessToken { get; }
    AuthResponse? CurrentUser { get; }
    event Action<bool>? AuthenticationStateChanged;
    Task InitializeAsync();
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
}

public class AuthService : IAuthService, IAsyncDisposable
{
    private const string PersistentStorage = "localStorage";
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<AuthService>? _dotNetRef;
    private AuthResponse? _currentUser;
    private bool _isRefreshing;

    private const string AccessTokenKey = "privestio_token";
    private const string RefreshTokenKey = "privestio_refresh_token";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public bool IsAuthenticated =>
        _currentUser is not null && !string.IsNullOrEmpty(_currentUser.AccessToken);
    public string? AccessToken => _currentUser?.AccessToken;
    public AuthResponse? CurrentUser => _currentUser;
    public event Action<bool>? AuthenticationStateChanged;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", request);
            if (!response.IsSuccessStatusCode)
                return null;

            _currentUser = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (_currentUser is not null)
            {
                await StoreTokensAsync(_currentUser.AccessToken, _currentUser.RefreshToken);
                ApplyAuthenticatedState(_currentUser.AccessToken);
            }

            return _currentUser;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/register", request);
            if (!response.IsSuccessStatusCode)
                return null;

            _currentUser = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (_currentUser is not null)
            {
                await StoreTokensAsync(_currentUser.AccessToken, _currentUser.RefreshToken);
                ApplyAuthenticatedState(_currentUser.AccessToken);
            }

            return _currentUser;
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        // Attempt to revoke the refresh token server-side
        try
        {
            var refreshToken = await GetStoredTokenAsync(RefreshTokenKey);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _httpClient.PostAsJsonAsync(
                    "/api/v1/auth/revoke",
                    new RefreshTokenRequest { RefreshToken = refreshToken }
                );
            }
        }
        catch
        {
            // Best-effort revocation — don't block logout on failure
        }

        _currentUser = null;
        await ClearStoredTokensAsync();
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task InitializeAsync()
    {
        if (_dotNetRef is null)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("authFunctions.initialize", _dotNetRef);
        }

        var token = await GetStoredTokenAsync(AccessTokenKey);

        if (!string.IsNullOrEmpty(token))
        {
            if (IsTokenExpired(token))
            {
                // Access token expired — try to refresh
                var refreshed = await TryRefreshTokenAsync();
                if (!refreshed)
                {
                    await ClearStoredTokensAsync();
                }
                return;
            }

            ApplyAuthenticatedState(token);
            return;
        }

        ApplySignedOutState();
    }

    /// <summary>
    /// Attempts to refresh the access token using the stored refresh token.
    /// Returns true on success.
    /// </summary>
    public async Task<bool> TryRefreshTokenAsync()
    {
        if (_isRefreshing)
            return false;

        _isRefreshing = true;
        try
        {
            var refreshToken = await GetStoredTokenAsync(RefreshTokenKey);

            if (string.IsNullOrEmpty(refreshToken))
                return false;

            // Remove auth header for the refresh call (it uses the refresh token, not JWT)
            var savedAuth = _httpClient.DefaultRequestHeaders.Authorization;
            _httpClient.DefaultRequestHeaders.Authorization = null;

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "/api/v1/auth/refresh",
                    new RefreshTokenRequest { RefreshToken = refreshToken }
                );

                if (!response.IsSuccessStatusCode)
                    return false;

                _currentUser = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (_currentUser is null)
                    return false;

                await StoreTokensAsync(_currentUser.AccessToken, _currentUser.RefreshToken);
                ApplyAuthenticatedState(_currentUser.AccessToken);
                return true;
            }
            finally
            {
                // Restore auth header if refresh failed
                if (_currentUser is null && savedAuth is not null)
                    _httpClient.DefaultRequestHeaders.Authorization = savedAuth;
            }
        }
        catch
        {
            return false;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private static bool IsTokenExpired(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return true;

            var payload = parts[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var jsonBytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("exp", out var expElement))
                return true;

            var expiry = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64());
            return expiry <= DateTimeOffset.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private async Task StoreTokensAsync(string accessToken, string? refreshToken)
    {
        await _jsRuntime.InvokeVoidAsync($"{PersistentStorage}.setItem", AccessTokenKey, accessToken);

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _jsRuntime.InvokeVoidAsync(
                $"{PersistentStorage}.setItem",
                RefreshTokenKey,
                refreshToken
            );
        }
    }

    private async Task ClearStoredTokensAsync()
    {
        await _jsRuntime.InvokeVoidAsync($"{PersistentStorage}.removeItem", AccessTokenKey);
        await _jsRuntime.InvokeVoidAsync($"{PersistentStorage}.removeItem", RefreshTokenKey);
    }

    private async Task<string?> GetStoredTokenAsync(string key)
    {
        return await _jsRuntime.InvokeAsync<string?>($"{PersistentStorage}.getItem", key);
    }

    [JSInvokable]
    public async Task OnStorageChanged()
    {
        var token = await GetStoredTokenAsync(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            ApplySignedOutState();
            return;
        }

        if (IsTokenExpired(token))
        {
            var refreshed = await TryRefreshTokenAsync();
            if (!refreshed)
            {
                await ClearStoredTokensAsync();
                ApplySignedOutState();
            }

            return;
        }

        ApplyAuthenticatedState(token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef is null)
        {
            return;
        }

        await _jsRuntime.InvokeVoidAsync("authFunctions.dispose");
        _dotNetRef.Dispose();
        _dotNetRef = null;
    }

    private void ApplyAuthenticatedState(string token)
    {
        _currentUser = _currentUser is null
            ? new AuthResponse { AccessToken = token }
            : _currentUser with { AccessToken = token };
        SetAuthorizationHeader(token);
        AuthenticationStateChanged?.Invoke(true);
    }

    private void ApplySignedOutState()
    {
        _currentUser = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        AuthenticationStateChanged?.Invoke(false);
    }

    private void SetAuthorizationHeader(string token) =>
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
}
