using Microsoft.Playwright;

namespace Privestio.E2E.Tests;

[Trait("Category", "E2E")]
public abstract class PlaywrightTestBase : IAsyncLifetime
{
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
}
