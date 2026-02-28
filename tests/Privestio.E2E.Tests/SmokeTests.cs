using Microsoft.Playwright;
using Xunit;

namespace Privestio.E2E.Tests;

/// <summary>
/// Playwright E2E smoke test: login → create account → add transaction (Task 1.15).
/// These tests require a running application and a configured BASE_URL environment variable.
/// Run with: BASE_URL=https://localhost:5001 dotnet test tests/Privestio.E2E.Tests/
/// </summary>
[Trait("Category", "E2E")]
public class SmokeTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("BASE_URL") ?? "https://localhost:5001";

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

    [Fact(Skip = "Requires running application and Playwright browsers installed. Run with `dotnet playwright install`.")]
    public async Task Login_CreateAccount_AddTransaction_SmokeTest()
    {
        var page = _page!;

        // --- Step 1: Navigate to login page ---
        await page.GotoAsync($"{BaseUrl}/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill login form
        await page.FillAsync("input#email", "admin@privestio.test");
        await page.FillAsync("input#password", "Admin@Privestio123!");
        await page.ClickAsync("button[type=submit]");

        // Wait for redirect to accounts
        await page.WaitForURLAsync($"{BaseUrl}/accounts");

        // --- Step 2: Navigate to add account ---
        await page.ClickAsync("text=+ Add Account");
        await page.WaitForURLAsync($"{BaseUrl}/accounts/new");

        // Fill account form
        await page.FillAsync("fluent-text-field[label='Account name'] input", "Test Chequing E2E");
        await page.SelectOptionAsync("fluent-select[label='Account type']", "Banking");
        await page.SelectOptionAsync("fluent-select[label='Sub-type']", "Chequing");
        await page.ClickAsync("fluent-button[type=submit]");

        // Wait for redirect to account detail
        await page.WaitForURLAsync(new Regex($"{Regex.Escape(BaseUrl)}/accounts/[a-f0-9-]+"));

        // Verify account was created
        var heading = await page.TextContentAsync("h1");
        Assert.Contains("Test Chequing E2E", heading);

        // --- Step 3: Add a transaction ---
        await page.ClickAsync("text=+ Add Transaction");
        await page.WaitForURLAsync(new Regex($"{Regex.Escape(BaseUrl)}/accounts/.+/transactions/new"));

        await page.FillAsync("fluent-text-field[label='Description'] input", "E2E Test Transaction");
        await page.FillAsync("fluent-number-field[label='Amount'] input", "100.00");
        await page.SelectOptionAsync("fluent-select[label='Transaction type']", "Debit");
        await page.ClickAsync("fluent-button[type=submit]");

        // Should redirect back to account detail
        await page.WaitForURLAsync(new Regex($"{Regex.Escape(BaseUrl)}/accounts/[a-f0-9-]+$"));
    }
}
