using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.JSInterop;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    string? AccessToken { get; }
    AuthResponse? CurrentUser { get; }
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

    public bool IsAuthenticated => _currentUser is not null && !string.IsNullOrEmpty(_currentUser.AccessToken);
    public string? AccessToken => _currentUser?.AccessToken;
    public AuthResponse? CurrentUser => _currentUser;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", request);
            if (!response.IsSuccessStatusCode) return null;

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
            if (!response.IsSuccessStatusCode) return null;

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
        var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "privestio_token");
        if (!string.IsNullOrEmpty(token))
        {
            SetAuthorizationHeader(token);
        }
    }

    private async Task StoreTokenAsync(string token) =>
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "privestio_token", token);

    private void SetAuthorizationHeader(string token) =>
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
}
