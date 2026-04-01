namespace Privestio.E2E.Tests;

[Collection("E2E")]
[Trait("Category", "UIReview")]
public sealed class UiReviewCaptureTests : PlaywrightTestBase
{
    public UiReviewCaptureTests(AppHostFixture appHostFixture)
        : base(appHostFixture) { }

    [Fact]
    public async Task CaptureCoreSurfaces_ForGpt54UiReview()
    {
        await CaptureUiReviewPageAsync("/", "01-home", "home-hero");
        await CaptureUiReviewPageAsync("/login", "02-login", "login-shell");
        await CaptureUiReviewPageAsync("/register", "03-register", "register-shell");

        await RegisterAndReachAccountsAsync(displayName: "UI Review User");
        await CaptureCurrentUiReviewPageAsync("04-accounts", "accounts-page");

        await GotoRelativeAsync("/import");
        await CaptureCurrentUiReviewPageAsync("05-import", "import-page");
    }
}
