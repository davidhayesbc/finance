using Microsoft.Playwright;
using Xunit;

namespace Privestio.E2E.Tests;

/// <summary>
/// Playwright E2E smoke test: login → create account → add transaction (Task 1.15).
/// These tests require a running application and a configured BASE_URL environment variable.
/// Run with: BASE_URL=https://localhost:5001 dotnet test tests/Privestio.E2E.Tests/
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public class SmokeTests : PlaywrightTestBase
{
    public SmokeTests(AppHostFixture appHostFixture)
        : base(appHostFixture) { }

    [Fact]
    public async Task Login_CreateAccount_AddTransaction_SmokeTest()
    {
        var page = Page;

        await RegisterAndReachAccountsAsync(displayName: "Smoke Test User");

        // --- Step 2: Navigate to add account ---
        await page.ClickAsync("[aria-label='Add new account']");
        await page.WaitForURLAsync($"{BaseUrl}/accounts/new");

        // Fill account form
        await page.FillAsync("input#account-name", "Test Chequing E2E");
        await page.SelectOptionAsync("#account-type", "Banking");
        await page.SelectOptionAsync("#account-subtype", "Chequing");
        await page.ClickAsync("fluent-button[type=submit]");

        // Wait for redirect to account detail
        await page.WaitForURLAsync(new Regex($"{Regex.Escape(BaseUrl)}/accounts/[a-f0-9-]+"));

        // Verify account was created
        var heading = await page.TextContentAsync("h1");
        Assert.Contains("Test Chequing E2E", heading);

        // --- Step 3: Add a transaction ---
        await page.ClickAsync("text=Add Transaction");
        await page.WaitForURLAsync(
            new Regex($"{Regex.Escape(BaseUrl)}/accounts/.+/transactions/new")
        );

        await page.FillAsync("input#transaction-description", "E2E Test Transaction");
        await page.FillAsync("input#transaction-amount", "100.00");
        await page.SelectOptionAsync("#transaction-type", "Debit");
        await page.ClickAsync("fluent-button[type=submit]");

        // Should redirect back to account detail
        await page.WaitForURLAsync(new Regex($"{Regex.Escape(BaseUrl)}/accounts/[a-f0-9-]+$"));
    }
}
