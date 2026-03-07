using Microsoft.Playwright;

namespace Privestio.E2E.Tests;

[Trait("Category", "E2E")]
public abstract class PlaywrightTestBase : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    protected static string BaseUrl =>
        Environment.GetEnvironmentVariable("BASE_URL") ?? "https://localhost:5001";

    protected IPage Page => _page ?? throw new InvalidOperationException("Playwright page not initialized.");

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page is not null) await _page.CloseAsync();
        if (_context is not null) await _context.CloseAsync();
        if (_browser is not null) await _browser.CloseAsync();
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
}