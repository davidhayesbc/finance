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
    Task InitializeAsync();
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private AuthResponse? _currentUser;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public bool IsAuthenticated =>
        _currentUser is not null && !string.IsNullOrEmpty(_currentUser.AccessToken);
    public string? AccessToken => _currentUser?.AccessToken;
    public AuthResponse? CurrentUser => _currentUser;

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
                await StoreTokenAsync(_currentUser.AccessToken);
                SetAuthorizationHeader(_currentUser.AccessToken);
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
                await StoreTokenAsync(_currentUser.AccessToken);
                SetAuthorizationHeader(_currentUser.AccessToken);
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
        _currentUser = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "privestio_token");
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task InitializeAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            "privestio_token"
        );
        if (!string.IsNullOrEmpty(token))
        {
            if (IsTokenExpired(token))
            {
                await _jsRuntime.InvokeVoidAsync(
                    "localStorage.removeItem",
                    "privestio_token"
                );
                return;
            }

            _currentUser = new AuthResponse { AccessToken = token };
            SetAuthorizationHeader(token);
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
            var padded = payload.PadRight(
                payload.Length + (4 - payload.Length % 4) % 4,
                '='
            );
            var jsonBytes = Convert.FromBase64String(
                padded.Replace('-', '+').Replace('_', '/')
            );
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

    private async Task StoreTokenAsync(string token) =>
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "privestio_token", token);

    private void SetAuthorizationHeader(string token) =>
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
}
