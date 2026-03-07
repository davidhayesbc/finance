namespace Privestio.E2E.Tests;

[Collection("E2E")]
public class UiRegressionTests : PlaywrightTestBase
{
    public UiRegressionTests(AppHostFixture appHostFixture)
        : base(appHostFixture) { }

    [Fact]
    public async Task HomePage_ShowsHeroAndFeatureCards()
    {
        await GotoRelativeAsync("/");

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("See your money clearly", heading);
        Assert.True(await Page.Locator("text=Offline-first personal finance").IsVisibleAsync());
        Assert.True(
            await Page.Locator("text=Track every balance with less friction").IsVisibleAsync()
        );
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
        await RegisterAndReachAccountsAsync();

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Account workspace", heading);
    }

    [Fact]
    public async Task ImportPage_ShowsWorkflowHeader_WhenAuthenticated()
    {
        await RegisterAndReachAccountsAsync();
        await GotoRelativeAsync("/import");

        var heading = await Page.TextContentAsync("h1");
        Assert.Contains("Import transactions", heading);
        Assert.True(await Page.Locator("text=Step 1").IsVisibleAsync());
    }
}
