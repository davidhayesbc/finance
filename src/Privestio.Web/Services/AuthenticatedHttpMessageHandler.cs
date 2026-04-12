using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public sealed class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private const string AccessTokenKey = "privestio_token";
    private const string RefreshTokenKey = "privestio_refresh_token";
    private const string PersistentStorage = "localStorage";
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthenticatedHttpMessageHandler(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (!IsAuthEndpoint(request.RequestUri))
        {
            var accessToken = await GetAccessTokenAsync(request.RequestUri, cancellationToken);
            if (!string.IsNullOrWhiteSpace(accessToken) && request.Headers.Authorization is null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetAccessTokenAsync(
        Uri? requestUri,
        CancellationToken cancellationToken
    )
    {
        var accessToken = await GetStoredTokenAsync(AccessTokenKey);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        if (!IsTokenExpired(accessToken))
        {
            return accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            accessToken = await GetStoredTokenAsync(AccessTokenKey);

            if (!string.IsNullOrWhiteSpace(accessToken) && !IsTokenExpired(accessToken))
            {
                return accessToken;
            }

            return await RefreshAccessTokenAsync(requestUri, cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> RefreshAccessTokenAsync(
        Uri? requestUri,
        CancellationToken cancellationToken
    )
    {
        var refreshToken = await GetStoredTokenAsync(RefreshTokenKey);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var apiBaseUrl = _configuration["Api:BaseUrl"];
        var baseAddress = !string.IsNullOrWhiteSpace(apiBaseUrl)
            ? new Uri(apiBaseUrl, UriKind.Absolute)
            : requestUri is { IsAbsoluteUri: true }
                ? new Uri(requestUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute)
                : null;

        if (baseAddress is null)
        {
            return null;
        }

        using var refreshClient = new HttpClient { BaseAddress = baseAddress };
        var response = await refreshClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            await ClearStoredTokensAsync();
            return null;
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(
            cancellationToken: cancellationToken
        );

        if (authResponse is null || string.IsNullOrWhiteSpace(authResponse.AccessToken))
        {
            await ClearStoredTokensAsync();
            return null;
        }

        await _jsRuntime.InvokeVoidAsync(
            $"{PersistentStorage}.setItem",
            AccessTokenKey,
            authResponse.AccessToken
        );

        if (!string.IsNullOrWhiteSpace(authResponse.RefreshToken))
        {
            await _jsRuntime.InvokeVoidAsync(
                $"{PersistentStorage}.setItem",
                RefreshTokenKey,
                authResponse.RefreshToken
            );
        }

        return authResponse.AccessToken;
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

    private static bool IsAuthEndpoint(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return false;
        }

        var path = requestUri.IsAbsoluteUri ? requestUri.AbsolutePath : requestUri.OriginalString;
        return path.Contains("/api/v1/auth/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTokenExpired(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return true;
            }

            var payload = parts[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var jsonBytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("exp", out var expiryElement))
            {
                return true;
            }

            var expiry = DateTimeOffset.FromUnixTimeSeconds(expiryElement.GetInt64());
            return expiry <= DateTimeOffset.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
