# GPT-5.4 UI Review Setup

This repository now includes a small review harness so GPT-5.4 can inspect rendered UI artifacts instead of reviewing Razor files in the abstract.

## Included pieces

- Playwright capture test: `tests/Privestio.E2E.Tests/UiReviewCaptureTests.cs`
- Reusable Copilot prompt: `.github/prompts/ui-review-gpt-5-4.prompt.md`
- Manual GitHub Actions workflow: `.github/workflows/ui-review.yml`

## Review scope

The capture flow generates desktop and mobile screenshots for the most important first-pass surfaces:

1. Home (`/`)
2. Login (`/login`)
3. Register (`/register`)
4. Accounts (`/accounts`)
5. Import (`/import`)

These routes give GPT-5.4 coverage of first impression, authentication, and the core authenticated workflow.

## Generate screenshots locally

```powershell
dotnet build tests/Privestio.E2E.Tests/Privestio.E2E.Tests.csproj --configuration Release
pwsh tests/Privestio.E2E.Tests/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test tests/Privestio.E2E.Tests/Privestio.E2E.Tests.csproj --configuration Release --filter "Category=UIReview"
```

Notes:

- If `BASE_URL` is not set, the test fixture starts the Aspire AppHost automatically.
- Set `BASE_URL` to reuse an already running environment.
- Set `UI_REVIEW_OUTPUT_DIR` to override the default output location.

The screenshots are written to `artifacts/ui-review/` by default.

## Run in GitHub Actions

Use the manual `UI Review Capture` workflow. It builds the solution, installs Playwright Chromium, runs only the `UIReview` test category, and uploads the screenshots as an artifact.

## How to run the review with GPT-5.4

1. Generate or download the screenshots.
2. Open Copilot Chat with GPT-5.4.
3. Attach the images from `artifacts/ui-review/`.
4. Reference `#prompt:ui-review-gpt-5-4`.
5. Keep reasoning on low or medium for the first review pass.

## Review constraints

The review prompt is tuned to the guidance from the OpenAI GPT-5.4 frontend article, but adapted for this repository's existing Fluent UI-based application.

The reviewer should:

- treat screenshots as the visual source of truth
- preserve the established product language instead of forcing a wholesale redesign
- assess both desktop and mobile captures
- focus on visual hierarchy, spacing, typography, clutter, state clarity, and CTA clarity
- flag weak first-view composition, missing visual anchors, and sections doing too many jobs
- keep recommendations realistic for the current Blazor and Fluent UI stack

The reviewer should not:

- invent issues not visible in the screenshots
- recommend a marketing-site aesthetic that conflicts with the current app shell
- suggest ornamental motion or visual treatments without a functional purpose