using System.IO;
using System.Net.Http.Json;
using Microsoft.Playwright;

namespace Privestio.E2E.Tests;

[Trait("Category", "E2E")]
public abstract class PlaywrightTestBase : IAsyncLifetime
{
    private const string DefaultApiBaseUrl = "https://localhost:7292";
    private static readonly UiReviewViewport[] UiReviewViewports =
    [
        new("desktop", 1440, 1200),
        new("mobile", 393, 1180),
    ];

    private readonly AppHostFixture _appHostFixture;
    private readonly HttpClient _apiClient = new(
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        }
    );
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    protected PlaywrightTestBase(AppHostFixture appHostFixture)
    {
        _appHostFixture = appHostFixture;
    }

    protected string BaseUrl => _appHostFixture.BaseUrl;
    protected string ApiBaseUrl => Environment.GetEnvironmentVariable("API_BASE_URL") ?? DefaultApiBaseUrl;

    protected IPage Page =>
        _page ?? throw new InvalidOperationException("Playwright page not initialized.");

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true }
        );
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions { IgnoreHTTPSErrors = true }
        );
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page is not null)
            await _page.CloseAsync();
        if (_context is not null)
            await _context.CloseAsync();
        if (_browser is not null)
            await _browser.CloseAsync();
        _apiClient.Dispose();
        _playwright?.Dispose();
    }

    protected async Task GotoRelativeAsync(string relativePath)
    {
        await Page.GotoAsync($"{BaseUrl}{relativePath}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    protected async Task WaitForTestIdAsync(string testId)
    {
        await Page.GetByTestId(testId).WaitForAsync();
    }

    protected async Task CaptureUiReviewPageAsync(
        string relativePath,
        string artifactPrefix,
        string readyTestId
    )
    {
        await GotoRelativeAsync(relativePath);
        await WaitForTestIdAsync(readyTestId);
        await CaptureUiReviewArtifactsAsync(artifactPrefix);
    }

    protected async Task CaptureCurrentUiReviewPageAsync(string artifactPrefix, string readyTestId)
    {
        await WaitForTestIdAsync(readyTestId);
        await CaptureUiReviewArtifactsAsync(artifactPrefix);
    }

    protected string GetUiReviewArtifactsDirectory()
    {
        var configuredPath = Environment.GetEnvironmentVariable("UI_REVIEW_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath, GetRepositoryRoot());
        }

        return Path.Combine(GetRepositoryRoot(), "artifacts", "ui-review");
    }

    protected async Task LoginIfConfiguredAsync()
    {
        var email = Environment.GetEnvironmentVariable("TEST_LOGIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("TEST_LOGIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Set TEST_LOGIN_EMAIL and TEST_LOGIN_PASSWORD to run authenticated UI regression tests."
            );
        }

        await GotoRelativeAsync("/login");
        await Page.FillAsync("input#email", email);
        await Page.FillAsync("input#password", password);
        await Page.ClickAsync("button[type=submit]");
        await Page.WaitForURLAsync($"{BaseUrl}/accounts");
    }

    protected async Task RegisterAndReachAccountsAsync(
        string? displayName = null,
        string? email = null,
        string? password = null
    )
    {
        await RegisterAndAuthenticateAsync(displayName, email, password);
        await GotoRelativeAsync("/accounts");
        await Page.WaitForURLAsync($"{BaseUrl}/accounts");
    }

    protected async Task<TestAuthResponse> RegisterAndAuthenticateAsync(
        string? displayName = null,
        string? email = null,
        string? password = null
    )
    {
        var resolvedDisplayName = displayName ?? "E2E Test User";
        var resolvedEmail = email ?? $"e2e.{Guid.NewGuid():N}@example.test";
        var resolvedPassword = password ?? "Admin@Privestio123!";

        var authResponse = await _apiClient.PostAsJsonAsync(
            $"{ApiBaseUrl}/api/v1/auth/register",
            new TestRegisterRequest
            {
                DisplayName = resolvedDisplayName,
                Email = resolvedEmail,
                Password = resolvedPassword,
            }
        );

        authResponse.EnsureSuccessStatusCode();

        var auth = await authResponse.Content.ReadFromJsonAsync<TestAuthResponse>();
        if (auth is null)
        {
            throw new InvalidOperationException("Registration did not return an auth payload.");
        }

        await GotoRelativeAsync("/login");
        await Page.EvaluateAsync(
            "tokens => { sessionStorage.setItem('privestio_token', tokens.accessToken); if (tokens.refreshToken) { sessionStorage.setItem('privestio_refresh_token', tokens.refreshToken); } }",
            new { accessToken = auth.AccessToken, refreshToken = auth.RefreshToken }
        );

        return auth;
    }

    protected async Task<TestAccountResponse> CreateAccountViaApiAsync(
        string accessToken,
        string name,
        string accountType,
        string accountSubType,
        decimal openingBalance = 0m,
        string currency = "CAD"
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/api/v1/accounts/")
        {
            Content = JsonContent.Create(
                new TestCreateAccountRequest
                {
                    Name = name,
                    AccountType = accountType,
                    AccountSubType = accountSubType,
                    Currency = currency,
                    OpeningBalance = openingBalance,
                    OpeningDate = DateOnly.FromDateTime(DateTime.UtcNow),
                }
            ),
        };

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );

        using var response = await _apiClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

    return await response.Content.ReadFromJsonAsync<TestAccountResponse>()
            ?? throw new InvalidOperationException("Account creation did not return an account payload.");
    }

    protected async Task CreateTransactionViaApiAsync(
        string accessToken,
        Guid accountId,
        decimal amount,
        string description,
        string transactionType = "Debit",
        string currency = "CAD"
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/api/v1/transactions/")
        {
            Content = JsonContent.Create(
                new TestCreateTransactionRequest
                {
                    AccountId = accountId,
                    Date = DateTime.UtcNow,
                    Amount = amount,
                    Currency = currency,
                    Description = description,
                    TransactionType = transactionType,
                }
            ),
        };

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );

        using var response = await _apiClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task CaptureUiReviewArtifactsAsync(string artifactPrefix)
    {
        var outputDirectory = GetUiReviewArtifactsDirectory();
        Directory.CreateDirectory(outputDirectory);

        foreach (var viewport in UiReviewViewports)
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.EvaluateAsync("window.scrollTo(0, 0)");
            await Page.WaitForTimeoutAsync(150);

            var outputPath = Path.Combine(outputDirectory, $"{artifactPrefix}-{viewport.Name}.png");
            await Page.ScreenshotAsync(
                new PageScreenshotOptions
                {
                    Path = outputPath,
                    FullPage = true,
                    Animations = ScreenshotAnimations.Disabled,
                    Caret = ScreenshotCaret.Hide,
                }
            );
        }
    }

    private static string GetRepositoryRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private sealed record UiReviewViewport(string Name, int Width, int Height);

    protected sealed record TestAuthResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string? RefreshToken { get; init; }
    }

    protected sealed record TestRegisterRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
    }

    protected sealed record TestCreateAccountRequest
    {
        public string Name { get; init; } = string.Empty;
        public string AccountType { get; init; } = string.Empty;
        public string AccountSubType { get; init; } = string.Empty;
        public string Currency { get; init; } = "CAD";
        public decimal OpeningBalance { get; init; }
        public DateOnly OpeningDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    protected sealed record TestAccountResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    protected sealed record TestCreateTransactionRequest
    {
        public Guid AccountId { get; init; }
        public DateTime Date { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "CAD";
        public string Description { get; init; } = string.Empty;
        public string TransactionType { get; init; } = "Debit";
    }
}
