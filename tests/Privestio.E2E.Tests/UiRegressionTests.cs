using System.Text.RegularExpressions;

namespace Privestio.E2E.Tests;

[Collection("E2E")]
public class UiRegressionTests : PlaywrightTestBase
{
    public UiRegressionTests(AppHostFixture appHostFixture)
        : base(appHostFixture) { }

    [Fact]
    public async Task HomeRoute_ShowsOperationalDashboard()
    {
        await GotoRelativeAsync("/");

        await Page
            .GetByTestId("app-rail")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Dashboard", heading);
        Assert.True(await Page.Locator("text=Net worth").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Pressure").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Watchlist").First.IsVisibleAsync());
    }

    [Fact]
    public async Task LoginPage_ShowsSignInShell()
    {
        await GotoRelativeAsync("/login");

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Pick up where you left off.", heading);
        Assert.True(await Page.Locator("input#email").IsVisibleAsync());
        Assert.True(await Page.Locator("input#password").IsVisibleAsync());
    }

    [Fact]
    public async Task RegisterPage_AllowsFullLengthEmailInput()
    {
        await GotoRelativeAsync("/register");

        var email = $"{new string('a', 242)}@example.com";
        await Page.FillAsync("input#email", email);

        var value = await Page.InputValueAsync("input#email");
        Assert.Equal(254, value.Length);
    }

    [Fact]
    public async Task UnknownRoute_ShowsFriendlyEmptyState()
    {
        await GotoRelativeAsync("/definitely-not-a-route");

        Assert.True(
            await Page.Locator("text=Sorry, there's nothing at this address.").IsVisibleAsync()
        );
    }

    [Fact]
    public async Task AccountsPage_ShowsWorkspaceHeader_WhenAuthenticated()
    {
        await GotoRelativeAsync("/accounts");

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Accounts", heading);
        Assert.True(await Page.Locator("text=Financial posture").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Net worth history").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Data quality").First.IsVisibleAsync());
    }

    [Fact]
    public async Task AccountDetailPage_ShowsDossierStructure_WhenNavigatingFromAccounts()
    {
        var auth = await RegisterAndAuthenticateAsync(
            displayName: "Account Detail Regression User"
        );
        var account = await CreateAccountViaApiAsync(
            auth.AccessToken,
            "Account Detail Regression Chequing",
            "Banking",
            "Chequing"
        );

        await GotoRelativeAsync($"/accounts/{account.Id}");

        var heading = await Page.TextContentAsync("h1");
        Assert.NotNull(heading);
        Assert.Contains("Account Detail Regression Chequing", heading);
        Assert.True(await Page.Locator("text=Identity").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=History").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Evidence").First.IsVisibleAsync());
    }

    [Fact]
    public async Task AccountsPage_AllowsRowDrillIn_ToAccountDetail()
    {
        var auth = await RegisterAndAuthenticateAsync(
            displayName: "Accounts Row Drilldown User"
        );
        var account = await CreateAccountViaApiAsync(
            auth.AccessToken,
            "Accounts Row Drilldown Chequing",
            "Banking",
            "Chequing"
        );

        await GotoRelativeAsync("/accounts");
        await Page.GetByTestId("accounts-page").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        var openButtonId = $"accounts-open-{account.Id:N}";
        var rowLink = Page.Locator($"#{openButtonId}").First;
        await rowLink.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
        await rowLink.ClickAsync();

        await Page.WaitForURLAsync(new Regex($"{Regex.Escape(BaseUrl)}/accounts/{account.Id}.*"));
        Assert.True(await Page.GetByTestId("account-detail-page").IsVisibleAsync());
    }

    [Fact]
    public async Task AccountDetailPage_ShowsRefreshDate_AndSortableTransactionGrid()
    {
        var auth = await RegisterAndAuthenticateAsync(
            displayName: "Account Detail Transactions User"
        );
        var account = await CreateAccountViaApiAsync(
            auth.AccessToken,
            "Account Detail Transactions Chequing",
            "Banking",
            "Chequing"
        );
        await CreateTransactionViaApiAsync(
            auth.AccessToken,
            account.Id,
            amount: 42.10m,
            description: "Regression Transaction"
        );

        await GotoRelativeAsync($"/accounts/{account.Id}");
    await Page.GetByTestId("account-detail-page").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        Assert.True(await Page.Locator("text=Refresh date").First.IsVisibleAsync());
        await Page.GetByTestId("account-transactions-panel").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
        await Page.GetByTestId("account-transactions-grid").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
        Assert.True(await Page.GetByTestId("account-transactions-type-filter").IsVisibleAsync());

        var amountSort = Page.Locator("button[aria-label='Sort by amount']").First;
        Assert.True(await amountSort.IsVisibleAsync());
        await amountSort.ClickAsync();
    }

    [Fact]
    public async Task ImportPage_ShowsWorkflowHeader_WhenAuthenticated()
    {
        await GotoRelativeAsync("/import");

        await Page
            .GetByTestId("app-rail")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Import", heading);
        Assert.True(await Page.Locator("text=Workflow context").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Upload").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Errors and exceptions").First.IsVisibleAsync());
    }

    [Fact]
    public async Task BudgetsPage_ShowsMonthReviewStructure_WhenAuthenticated()
    {
        await GotoRelativeAsync("/budgets");

        await Page
            .GetByTestId("app-rail")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Budgets", heading);
        Assert.True(await Page.Locator("text=This month").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Category performance").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=At risk").First.IsVisibleAsync());
    }

    [Fact]
    public async Task ForecastPage_ShowsProjectionPlane_WhenAuthenticated()
    {
        await GotoRelativeAsync("/forecast");

        await Page
            .GetByTestId("app-rail")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Forecast", heading);
        Assert.True(await Page.Locator("text=Projection").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Drivers").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Monthly periods").First.IsVisibleAsync());
    }

    [Theory]
    [InlineData("/dashboard", "dashboard-page")]
    [InlineData("/accounts", "accounts-page")]
    [InlineData("/import", "import-page")]
    [InlineData("/budgets", "budgets-page")]
    [InlineData("/forecast", "forecast-page")]
    public async Task CoreRoutes_ExposeSharedPageRegions(string route, string pageTestId)
    {
        await GotoRelativeAsync(route);

        await Page
            .GetByTestId(pageTestId)
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        Assert.True(await Page.Locator("[data-page-region='orientation']").First.IsVisibleAsync());
        Assert.True(await Page.Locator("[data-page-region='primary']").First.IsVisibleAsync());
        Assert.True(await Page.Locator("[data-page-region='supporting']").First.IsVisibleAsync());
        Assert.True(await Page.Locator("[data-page-region='exceptions']").First.IsVisibleAsync());
    }

    [Fact]
    public async Task DashboardPage_ShowsOperationalRegions()
    {
        await GotoRelativeAsync("/dashboard");

        await Page
            .GetByTestId("app-rail")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
        await Page
            .GetByTestId("app-utility-strip")
            .WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

        Assert.True(await Page.GetByTestId("app-rail").IsVisibleAsync());
        Assert.True(await Page.GetByTestId("app-utility-strip").IsVisibleAsync());

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Dashboard", heading);
        Assert.True(await Page.Locator("text=Net worth").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Composition").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Pressure").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Watchlist").First.IsVisibleAsync());
        Assert.True(await Page.Locator("text=Accounts").First.IsVisibleAsync());
    }
}
