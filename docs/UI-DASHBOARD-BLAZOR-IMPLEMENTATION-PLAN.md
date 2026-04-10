# Privestio Dashboard Blazor Implementation Plan

## Purpose

Translate [UI-DASHBOARD-CONCEPT-SPEC.md](./UI-DASHBOARD-CONCEPT-SPEC.md) into an implementation sequence for the current Blazor web app.

This plan is intentionally scoped to the dashboard page and the minimal shared primitives needed to establish the new screen scaffold.

## Target Outcome

Ship the first dashboard rewrite slice with:

- a new dashboard page scaffold that matches the concept spec
- stable named regions for orientation, posture, net worth plane, composition, pressure, watchlist, and accounts
- placeholder-ready rendering for partial and empty data states
- CSS structure that moves the page away from card stacks and toward bands and workbench planes

## Current Owning Files

- [Dashboard.razor](./../src/Privestio.Web/Pages/Dashboard.razor)
- [PageHeader.razor](./../src/Privestio.Web/Components/PageHeader.razor)
- [HistoryRangeSelector.razor](./../src/Privestio.Web/Components/HistoryRangeSelector.razor)
- [app.css](./../src/Privestio.Web/wwwroot/css/app.css)
- [UiRegressionTests.cs](./../tests/Privestio.E2E.Tests/UiRegressionTests.cs)

## Delivery Sequence

### 1. Lock The Dashboard Scaffold With A Narrow E2E Check

- Add a dashboard-specific authenticated regression test.
- Assert the rewritten page exposes the named regions from the concept spec.
- Use this test as the fast guardrail while reshaping the page markup.

### 2. Refactor The Dashboard Page Into Named Regions

- Replace the current summary-card stack in [Dashboard.razor](./../src/Privestio.Web/Pages/Dashboard.razor).
- Introduce page-level regions in this order:
- orientation band
- posture strip
- net worth plane
- composition and pressure band
- watchlist
- accounts ledger summary
- Keep the page renderable even when some API calls return no data.

### 3. Add Dashboard-Specific View State

- Add a selected history range state.
- Reuse [HistoryRangeSelector.razor](./../src/Privestio.Web/Components/HistoryRangeSelector.razor) for the chart context control.
- Derive lightweight watchlist and pressure summaries from currently available data rather than blocking on new backend endpoints.
- Prefer explicit placeholder values and empty-region copy over conditional layout collapse.

### 4. Introduce Dashboard Region Styling In The Shared CSS

- Add dashboard-specific classes in [app.css](./../src/Privestio.Web/wwwroot/css/app.css).
- Move the page from rounded card clusters toward banded layout regions.
- Create dedicated styles for:
- dashboard grid
- posture strip metrics
- chart plane header
- composition and pressure split band
- watchlist rows
- accounts summary table region
- Keep changes local to dashboard classes to avoid unintended regressions in other screens.

### 5. Preserve Existing Working Components Where Useful

- Keep [PageHeader.razor](./../src/Privestio.Web/Components/PageHeader.razor) for the orientation band initially.
- Keep existing chart components for the first scaffold pass.
- Keep existing `FluentDataGrid` usage for the accounts summary while changing its surrounding composition.
- Avoid introducing new reusable components until the page structure is proven.

### 6. Normalize Loading, Empty, And Partial-Failure States

- Keep the orientation band visible while data loads.
- Render posture placeholders during loading instead of a page-level spinner-only experience.
- Keep the chart frame visible for empty and failed history states.
- Keep watchlist and accounts regions visible with explicit empty copy.

### 7. Validate The Slice

- Run the narrow dashboard regression test after the first scaffold pass.
- Run a focused compile or test command for the touched slice if needed.
- Verify the dashboard route still loads when authenticated and remains structurally coherent with no data.

## Initial Implementation Boundaries

This first slice should do all of the following:

- establish the new dashboard region layout
- replace card-mosaic composition on the dashboard page
- add a range selector and region labels aligned to the concept spec
- add placeholder watchlist and pressure regions

This first slice does not need to do all of the following:

- complete the global shell rewrite
- introduce the final alert drawer
- deliver final chart rendering behavior
- add new backend API contracts
- finish the final visual polish across the rest of the app

## Completion Criteria For This Slice

- The dashboard route renders the new named regions from the concept spec.
- The dominant chart plane sits above composition, watchlist, and account summary regions.
- The page no longer reads as a stack of independent cards.
- The dashboard has a narrow automated regression check.
- The page compiles and the focused test passes.

## Next Slice After This One

After the scaffold lands, the next dashboard slice should focus on:

1. data-quality-driven watchlist logic
2. pressure-band metrics that are less placeholder-driven
3. chart-plane polish and transition behavior
4. eventual shell integration with the future left-rail layout
