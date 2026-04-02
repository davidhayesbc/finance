# Privestio UI Content Plan

## Purpose

This document translates the visual thesis in [UI-VISUAL-THESIS.md](UI-VISUAL-THESIS.md) into a content and hierarchy plan for the core product screens.

The goal is not to define final components. The goal is to decide what each screen must say, in what order, and with what dominant visual idea.

## Product UI Rules

These five screens are operational product surfaces, not marketing pages.

- No homepage hero patterns inside the application
- No dashboard tile farm
- No decorative imagery inside routine workspaces
- No section without a decision-making purpose
- No copy that sounds like advertising when utility copy would do the job better

Every screen should answer three questions within the first few seconds:

1. Where am I?
2. What changed?
3. What should I do next?

## Shared Frame

Each core screen should inherit the same structural rhythm.

1. Orientation band
2. Primary working plane
3. Supporting evidence or detail
4. Actionable exceptions, history, or definitions

### Orientation Band

This is the top zone beneath the app chrome.

It should contain:

- page title
- one-line operational description
- date range, month, horizon, or context selector when needed
- one or two high-value actions only

It should not contain:

- marketing statements
- decorative KPI cards
- more than one line of explanatory copy

### Primary Working Plane

This is the largest surface on the page. It owns the screen.

Examples:

- trend chart on dashboard
- ledger table on accounts
- mapping workspace on import
- category performance table on budgets
- forecast trajectory on forecast

### Supporting Evidence

This is where comparison, segmentation, or diagnostic context lives.

Examples:

- asset allocation
- institutions and account mix
- import errors and duplicates
- budget overages
- forecast drivers and inflection points

### Actionable Exceptions

This zone is for what needs intervention.

Examples:

- stale prices
- import failures
- categories over budget
- months with projected negative balance
- accounts with missing or suspicious data

## Dashboard

### Job

Tell the user the current financial posture of the household in one glance, then direct attention to the few changes that matter.

### Dominant Visual Idea

One wide net-worth plane with strong temporal depth. The chart is the screen's anchor, not the summary numbers.

### First-Glance Takeaway

"This is where I stand, how I got here, and what needs attention now."

### Content Sequence

1. Orientation band
Page title: Dashboard
Description: Current financial position, recent movement, and active issues.
Actions: Add transaction, sync prices, view notifications.

2. Posture strip
Content: net worth, assets, liabilities, last refresh, unresolved alerts count.
Purpose: establish status without turning the page into a KPI mosaic.

3. Net worth plane
Content: net worth trend with range selector and optional overlays for assets and liabilities.
Purpose: make trajectory the first serious read.

4. Allocation and pressure band
Content: asset allocation, debt share, cash concentration, or other structural composition views.
Purpose: explain what the household is made of, not just what it totals.

5. Accounts requiring attention
Content: stale balances, missing prices, overspent categories, failed imports, upcoming cash pressure.
Purpose: surface intervention points in one compact list.

6. Account ledger summary
Content: top accounts by balance, movement, and type.
Purpose: provide direct drill-in paths without replacing the dedicated Accounts workspace.

### Notes

- The chart should feel like the page, not a widget dropped into it.
- Asset allocation should not use a playful palette.
- Exception content should read as an operations list, not a notification wall.

### Utility Copy Direction

- Good headings: Net worth, Composition, Watchlist, Accounts
- Avoid headings: Your financial future, Wealth snapshot, Smart insights

## Accounts

### Job

Provide a disciplined index of all accounts, then make it easy to move into a specific account dossier.

### Dominant Visual Idea

A ledger hall: one long, authoritative table anchored by a slim trend band above it.

### First-Glance Takeaway

"These are all my accounts, how they are grouped, and where the imbalances or gaps are."

### Content Sequence

1. Orientation band
Page title: Accounts
Description: Review balances, institutions, types, and data freshness across the full account set.
Actions: Add account, sync market history.

2. Structural summary band
Content: total accounts, institutions, investment share, debt share, accounts with stale data.
Purpose: show the shape of the portfolio before the detail table.

3. Net worth history strip
Content: compact trend chart with range selector.
Purpose: place the account universe in time.

4. Primary ledger table
Content: account name, type, institution, balance, status, last updated, quick view action.
Purpose: be the main working surface for scanning and navigation.

5. Grouping or filter support
Content: by institution, by account type, by asset class, by ownership.
Purpose: help users reduce complexity without burying the table.

6. Data quality exceptions
Content: missing opening balance, stale market history, orphaned account state, empty accounts.
Purpose: turn account maintenance into a visible workflow.

### Detail Companion

The account detail screen should inherit dossier logic.

- opening band: account identity, institution, type, current balance, key status
- middle plane: balance or performance history depending on account type
- lower plane: transactions, holdings, valuations, or amortization detail

### Utility Copy Direction

- Good headings: Accounts, Institutions, Data freshness, Account detail
- Avoid headings: Portfolio universe, Money hubs, Wealth containers

## Import

### Job

Move the user through a high-confidence ingestion workflow where every step makes the next risk clearer.

### Dominant Visual Idea

A calibration bench: the screen feels procedural, exact, and reversible.

### First-Glance Takeaway

"I can see where I am in the import process, what the file contains, and what will happen before anything writes."

### Content Sequence

1. Orientation band
Page title: Import
Description: Upload, map, preview, and commit transaction files with full visibility before write.
Actions: Saved mappings, import history.

2. Persistent step rail
Content: upload, mapping, preview, complete.
Purpose: keep procedural orientation visible at all times.

3. Step workspace
Upload state: account selector, file intake, supported formats, detected format.
Mapping state: column mapping table, sample rows, advanced settings.
Preview state: import counts, duplicates, errors, sample outcomes.
Complete state: result summary and next action.

4. Right-side inspector or lower support panel
Content: selected account, mapping health, required fields status, plugin source, commit impact.
Purpose: keep decision context visible without crowding the main workspace.

5. Error and exception band
Content: row-level failures, duplicate policy, unsupported format, missing required mappings.
Purpose: make failure legible and actionable, not alarming.

### Notes

- This screen can use stronger framing than other pages because the workflow is sequential.
- The step indicator should feel architectural, not gamified.
- Success should feel controlled, not celebratory.

### Utility Copy Direction

- Good headings: Source file, Column mapping, Preview results, Errors, Commit
- Avoid headings: Almost there, Magic import, Clean up your data fast

## Budgets

### Job

Show the posture of the current month, which categories are healthy or stressed, and where budget definitions need adjustment.

### Dominant Visual Idea

A month ledger with pressure indicators. The category table owns the screen.

### First-Glance Takeaway

"This month is on track here, drifting here, and failing here."

### Content Sequence

1. Orientation band
Page title: Budgets
Description: Track the current month against category limits, rollover rules, and actual spend.
Actions: Add budget.

2. Period selector strip
Content: month, year, quick jump to current period, refresh.
Purpose: establish time context before interpretation.

3. Monthly posture band
Content: total budgeted, total spent, remaining, categories over budget, rollover exposure.
Purpose: read the month before reading individual categories.

4. Primary category performance table
Content: category, budgeted, actual, remaining, percent used, status.
Purpose: deliver the main judgment surface for the month.

5. Pressure list
Content: over-budget categories, categories at risk, categories with no budget, unusual spend spikes.
Purpose: isolate interventions from the main table.

6. Budget definitions and editing surface
Content: recurring definitions, rollover settings, notes, create or edit form.
Purpose: separate measurement from setup while keeping both on the same page.

### Notes

- Progress bars should feel analytical, not gamified.
- Over-budget states should be sharp and obvious, but not neon.
- The create or edit form should appear as a deliberate side sheet or lower editing band rather than a floating interruption.

### Utility Copy Direction

- Good headings: This month, Category performance, At risk, Budget definitions
- Avoid headings: Spending smarter, Stay on target, Your monthly plan

## Forecast

### Job

Show where cash flow is heading, when it tightens, and which assumptions are driving the outcome.

### Dominant Visual Idea

One projection plane with a clear forward horizon and marked stress points.

### First-Glance Takeaway

"If nothing changes, this is where my balances go and these are the months that matter."

### Content Sequence

1. Orientation band
Page title: Forecast
Description: Project balances from recurring income, expenses, budgets, and sinking-fund contributions.
Actions: change horizon, change scenario, refresh.

2. Forecast posture band
Content: total projected income, total projected expenses, net projected, end balance, lowest projected balance.
Purpose: summarize the horizon before chart reading.

3. Projection plane
Content: balance trajectory across the selected horizon with threshold markers and flagged low points.
Purpose: own the page with forward-looking clarity.

4. Drivers and inflection band
Content: largest expense pressures, months with negative net, sinking fund spikes, recurring income changes.
Purpose: explain why the line bends.

5. Period ledger
Content: monthly breakdown table for income, expenses, budgeted expenses, sinking funds, net, balance.
Purpose: give the user a precise audit trail for the forecast.

6. Assumptions and intervention surface
Content: scenario selection, growth assumptions, contribution changes, alerts for minimum balance risk.
Purpose: convert the page from passive reading into scenario planning.

### Notes

- The current bar-chart treatment should evolve into a cleaner trajectory view.
- This page should feel analytical, not optimistic.
- Risk months should be visually marked but not dramatized.

### Utility Copy Direction

- Good headings: Projection, Drivers, Monthly periods, Assumptions
- Avoid headings: Future outlook, Your money path, Plan ahead with confidence

## Cross-Screen Copy Model

Use utility language consistently.

- Prefer: current, projected, remaining, imported, stale, unresolved, over budget, due, synced
- Avoid: amazing, smart, effortless, personalized, empowering, seamless

Section descriptions should usually be one sentence. Labels should be plain enough that a user scanning only titles, numbers, and row labels can still understand the page.

## Layout Priority Order

When space gets tight on tablet or mobile, preserve this order:

1. orientation band
2. primary working plane
3. actionable exceptions
4. supporting evidence
5. secondary definitions or editing surfaces

## Next Design Deliverable

The next document should be an interaction thesis for these same screens covering:

1. page transitions
2. chart reveal behavior
3. table and filter interactions
4. exception-state emphasis
5. drawer, dialog, and side-sheet behavior
