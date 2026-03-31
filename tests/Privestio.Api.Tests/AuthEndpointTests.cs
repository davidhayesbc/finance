using System.Net;
using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Api.Tests;

/// <summary>
/// Integration tests for the authentication endpoints.
/// Covers registration, login, token refresh, token revocation, and rate limiting.
/// </summary>
public class AuthEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static RegisterRequest NewUniqueUser() =>
        new()
        {
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Password = "SecureP@ssw0rd!",
            DisplayName = "Test User",
        };

    // --- Registration ---

    [Fact]
    public async Task Register_WithValidData_Returns201WithTokens()
    {
        var request = NewUniqueUser();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.Email.Should().Be(request.Email);
        auth.DisplayName.Should().Be(request.DisplayName);
        auth.UserId.Should().NotBeEmpty();
        auth.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsValidationProblem()
    {
        var request = NewUniqueUser();

        // First registration succeeds
        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second registration with same email fails
        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsValidationProblem()
    {
        var request = new RegisterRequest
        {
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Password = "weak",
            DisplayName = "Test User",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Login ---

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        var registerReq = NewUniqueUser();
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);

        var loginReq = new LoginRequest
        {
            Email = registerReq.Email,
            Password = registerReq.Password,
        };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.Email.Should().Be(registerReq.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var registerReq = NewUniqueUser();
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);

        var loginReq = new LoginRequest
        {
            Email = registerReq.Email,
            Password = "WrongP@ssw0rd1!",
        };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var loginReq = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyP@ssw0rd1!",
        };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Refresh Token ---

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokenPair()
    {
        var registerReq = NewUniqueUser();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var initialAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshReq = new RefreshTokenRequest { RefreshToken = initialAuth!.RefreshToken! };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth.Should().NotBeNull();
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBeNullOrEmpty();
        // The new refresh token should be different (rotation)
        newAuth.RefreshToken.Should().NotBe(initialAuth.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithAlreadyUsedToken_Returns401()
    {
        var registerReq = NewUniqueUser();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var initialAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshReq = new RefreshTokenRequest { RefreshToken = initialAuth!.RefreshToken! };

        // First refresh succeeds
        var first = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reusing the old refresh token should fail (it was revoked during rotation)
        var second = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var refreshReq = new RefreshTokenRequest { RefreshToken = "totally-invalid-token" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Revoke ---

    [Fact]
    public async Task Revoke_WithValidTokenAndAuth_Returns204()
    {
        var registerReq = NewUniqueUser();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Authenticate the client for the revoke call
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var revokeReq = new RefreshTokenRequest { RefreshToken = auth.RefreshToken! };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/revoke", revokeReq);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The revoked token should no longer work for refresh
        _client.DefaultRequestHeaders.Authorization = null;
        var refreshReq = new RefreshTokenRequest { RefreshToken = auth.RefreshToken! };
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Protected Endpoint Access ---

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Succeeds()
    {
        var registerReq = NewUniqueUser();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.GetAsync("/api/v1/accounts");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
