using System.IO;
using Microsoft.Playwright;

namespace Privestio.E2E.Tests;

[Trait("Category", "E2E")]
public abstract class PlaywrightTestBase : IAsyncLifetime
{
    private static readonly UiReviewViewport[] UiReviewViewports =
    [
        new("desktop", 1440, 1200),
        new("mobile", 393, 1180),
    ];

    private readonly AppHostFixture _appHostFixture;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    protected PlaywrightTestBase(AppHostFixture appHostFixture)
    {
        _appHostFixture = appHostFixture;
    }

    protected string BaseUrl => _appHostFixture.BaseUrl;

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
        var resolvedDisplayName = displayName ?? "E2E Test User";
        var resolvedEmail = email ?? $"e2e.{Guid.NewGuid():N}@example.test";
        var resolvedPassword = password ?? "Admin@Privestio123!";

        await GotoRelativeAsync("/register");
        await Page.FillAsync("input#displayName", resolvedDisplayName);
        await Page.FillAsync("input#email", resolvedEmail);
        await Page.FillAsync("input#password", resolvedPassword);
        await Page.ClickAsync("button[type=submit]");
        await Page.WaitForURLAsync($"{BaseUrl}/accounts");
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
}
