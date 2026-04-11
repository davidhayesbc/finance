# Privestio UI Rewrite Checklist

## Purpose

This document translates a live Playwright review of the current Privestio UI into a full rewrite checklist aligned to:

- [UI-VISUAL-THESIS.md](./UI-VISUAL-THESIS.md)
- [UI-CONTENT-PLAN.md](./UI-CONTENT-PLAN.md)
- [UI-INTERACTION-THESIS.md](./UI-INTERACTION-THESIS.md)

Backwards compatibility and phased migration are intentionally ignored. The target is a coherent replacement UI, not an incremental restyle.

## Current-State Findings From Live Review

- [x] Replace the current overview page with a true operational dashboard. The current `/` route behaves like a product landing page with hero copy and feature cards instead of a household finance command surface.
- [ ] Replace the current shell aesthetic. The live UI uses soft SaaS gradients, oversized rounded corners, pill navigation, blue accents, and frosted cards that conflict with the thesis of paper light, graphite framing, smoked glass, and verdigris accent.
- [x] Replace the top navigation model. The current header is horizontally oriented and collapses the authenticated product into nav pills instead of providing the architectural left rail called for in the visual thesis.
- [ ] Remove card-grid composition as the dominant layout pattern. The current UI repeatedly stacks `FluentCard` surfaces instead of using wide bands, split workspaces, dossier layouts, and dense table planes.
- [x] Rebuild the import workflow as a workbench. The current page exposes the steps, but it still reads as stacked forms and cards rather than a calibration bench with a persistent step frame and secondary inspector.
- [x] Rebuild accounts as a dossier-led workspace. The current account page is closer than the landing page, but it still relies on isolated cards instead of a ledger hall with strong structural hierarchy.
- [x] Rebuild forecast as a projection plane. The current forecast uses a summary card, table, and bar chart; the target calls for a single forward trajectory surface with stress markers and clearer causal drivers.
- [x] Replace the budgets page entirely. The live route throws an exception before rendering useful content, and its current shape is not aligned to the target month-led pressure review.
- [ ] Introduce an intentional interaction system. The live app reads mostly as static page swaps and default control behavior rather than weighted, orientation-preserving motion.
- [ ] Normalize empty, loading, and error states. Current states are generic and card-bound; target states should preserve the page frame and feel like part of one continuous instrument.

## Rewrite Sequence

### 1. Replace The App Shell And Design Tokens

- [x] Delete the current homepage-first mental model and define the authenticated product shell as the default design center.
- [x] Replace the current blue-forward token set with the thesis palette: bone, oat, graphite, coal, verdigris, gain, risk, and forecast tones.
- [x] Replace the current typography stack with a two-family system: Familjen Grotesk for operational UI and Newsreader for selective high-importance figures and headings.
- [ ] Replace the current rounded, glossy shape language with near-square corners, thin dividers, flatter modules, and selective smoked-glass overlays.
- [x] Reduce decorative shadows until the layout still works without them.
- [ ] Define a single global spacing and density system tuned for dense analytical surfaces rather than marketing cards.
- [x] Create a left navigation rail that feels architectural and remains stable across page changes.
- [x] Move status, theme control, sync state, notifications, and profile actions into a restrained top utility strip instead of giving them equal weight with primary navigation.
- [x] Ensure the app frame keeps navigation chrome stable while only the content plane changes.
- [x] Remove hero-style headlines and promotional support copy from authenticated product routes.

### 2. Build Shared Page Structure Components

- [ ] Standardize every core page around four regions: orientation band, primary working plane, supporting evidence, and actionable exceptions.
- [ ] Rebuild the page header component so it supports a short utility description, one context selector, and at most one or two high-value actions.
- [ ] Create reusable band components for posture strips, risk lists, allocation or composition bands, and analytic support zones.
- [ ] Create a reusable right-side inspector or side-sheet pattern for editing, drill-in, and exception review.
- [ ] Create a dense table surface standard with consistent header treatment, row density, hover tone, active-row styling, and inline actions.
- [ ] Create a shared chart frame with consistent title, range control placement, tooltip behavior, empty states, and loading behavior.
- [ ] Create localized loading patterns that preserve layout structure instead of replacing whole pages with spinners.
- [ ] Create empty states that explain what is missing, what action unlocks data, and where the user remains in the workflow.
- [ ] Create error states that attach to the affected surface first and only escalate to page-level bars when the whole workflow is blocked.

### 3. Replace Overview With Dashboard

- [x] Remove the current hero section and feature-card content from the `/` route.
- [x] Rename the route concept from Overview to Dashboard in the primary information architecture.
- [x] Build a dashboard orientation band with title, one-line operational description, range selector, and high-value actions only.
- [x] Build a posture strip for net worth, assets, liabilities, last refresh, and unresolved alerts.
- [x] Make the net-worth plane the dominant visual surface on the page.
- [ ] Add optional overlays for assets and liabilities without turning the chart into a decorative widget.
- [x] Add an allocation and pressure band explaining what the household is made of, such as asset allocation, debt share, and cash concentration.
- [x] Add a compact watchlist that surfaces stale prices, missing balances, overspent categories, failed imports, and forecast pressure.
- [x] Add an account summary list that supports direct drill-in to accounts without competing with the accounts workspace.
- [x] Ensure the dashboard feels like arrival into a serious instrument, not a landing page after sign-in.

### 4. Rebuild Accounts As A Ledger Hall

- [x] Keep accounts as a first-class route in the main rail.
- [x] Rework the page into a ledger hall with a slim history band above a dominant account table.
- [x] Expand the structural summary band to include total accounts, institutions, investment share, debt share, and stale-data count.
- [x] Keep the compact net-worth history strip above the ledger rather than surrounding it with multiple separate cards.
- [x] Rebuild the primary ledger table to emphasize account name, type, institution, balance, freshness, and quick view actions with calm row styling.
- [x] Add grouping and filtering views by institution, account type, asset class, and ownership without displacing the table.
- [x] Add a visible data-quality exceptions zone for stale history, missing opening balances, empty accounts, and orphaned records.
- [x] Rebuild account detail pages as dossiers with identity band, history or performance plane, and lower transaction or holdings evidence.
- [ ] Use side sheets for quick detail and small edits instead of routing to full replacement pages for every minor action.
- [ ] Preserve scroll position and filter context during account-level drill-in and return.

### 5. Rebuild Import As A Calibration Bench

- [x] Keep import as a sequential workflow, but replace the current stacked-card presentation with a stable workbench frame.
- [x] Build a persistent step rail showing upload, mapping, preview, and complete at all times.
- [x] Keep the main step workspace in the center plane and a persistent inspector on the side for account context, mapping health, required fields, plugin source, and commit impact.
- [x] Rebuild upload so the user sees file source, supported formats, detected format, and account context in one structured frame.
- [x] Rebuild mapping so sample rows and field assignments remain aligned while the user edits.
- [x] Move advanced settings into an intentional inspector or disclosure region instead of a generic details element.
- [x] Rebuild preview so counts, duplicates, and errors feel like an operational readout rather than a stat-card block.
- [x] Surface row-level errors inline with the affected result set.
- [x] Make commit actions feel deliberate and controlled, not merely like the final button at the bottom of a form.
- [x] Rebuild success state so it confirms the write, points to the next useful action, and keeps the workflow frame intact.

### 6. Rebuild Budgets As A Month Pressure Review

- [x] Replace the current budgets route implementation from scratch; the live page currently throws before rendering a usable surface.
- [x] Build a month-oriented orientation band with month selector, current-period jump, and one high-value create action.
- [x] Add a monthly posture band for total budgeted, total spent, remaining, over-budget categories, and rollover exposure.
- [x] Make the category performance table the dominant surface on the page.
- [x] Present category, budgeted, actual, remaining, percent used, and status in a dense, calm ledger rather than in isolated widgets.
- [x] Replace playful or generic progress treatments with analytical pressure indicators.
- [x] Add a pressure list for over-budget categories, categories at risk, missing budgets, and unusual spend spikes.
- [x] Move budget creation and editing into a side sheet or anchored editing band rather than a card dropped into page flow.
- [x] Separate measurement from setup by keeping budget definitions present but subordinate to the current-month judgment surface.
- [ ] Ensure budget threshold changes sharpen instantly and locally without banner-heavy feedback.

### 7. Rebuild Forecast As A Projection Plane

- [x] Replace the current summary-plus-bar-chart composition with a single dominant trajectory plane.
- [x] Build a forecast orientation band with horizon selector, scenario selector, and refresh action.
- [x] Add a posture band with projected income, projected expenses, net projected, end balance, and lowest projected balance.
- [x] Make the projection chart the primary working plane and keep the frame stable during horizon changes.
- [x] Replace vertical bars with a forward-looking balance line or area treatment that emphasizes direction and pressure over categorical comparison.
- [x] Add explicit risk markers for low months, threshold crossings, and negative periods.
- [x] Add a drivers and inflection band explaining the largest expense pressures, sinking-fund spikes, income changes, and turning points.
- [x] Keep the monthly ledger below the projection as the audit trail for the forecast.
- [ ] Move assumptions and scenario editing into a layered panel so the main projection remains visible.
- [ ] Ensure the page feels analytical and slightly tense rather than optimistic, decorative, or celebratory.

### 8. Introduce The Interaction System

- [ ] Implement a settling entrance for page structure: orientation band first, primary plane second, supporting evidence last.
- [ ] Implement quiet page transitions that preserve the shell and change only the content area.
- [ ] Implement data recomposition for range, filter, grouping, month, and scenario changes so charts and tables update in place.
- [ ] Implement side-sheet and inspector transitions with firm tracked movement and immediate readable content.
- [ ] Replace abrupt full-screen loading transitions with localized dim-and-refresh behavior.
- [ ] Add consistent hover and focus behavior for dense tables that improves scanning without glow-heavy decoration.
- [ ] Ensure chart reveal behavior is structural: frame first, data second, labels last.
- [ ] Ensure urgent states use one-time emphasis only; never loop attention-grabbing animation on financial data.
- [ ] Respect reduced motion by replacing most movement with quick fades or instant state changes.
- [ ] Preserve focus and invoking context after every drawer, dialog, step change, and drill-in.

### 9. Rebuild Exception, Loading, And Feedback Patterns

- [ ] Create a severity model shared across the app: informational, advisory, and urgent.
- [ ] Attach warnings to the affected number, row, month, or dataset before introducing banners.
- [ ] Rebuild watchlists, stale-data notices, import failures, and forecast warnings as compact operational exception rows.
- [ ] Keep success feedback short and local to the affected control or surface.
- [ ] Ensure pending states live on the button, table, chart, or filter that caused the wait.
- [ ] Replace generic empty-state wording with action-led utility copy.
- [ ] Ensure semantic colors behave as data signals rather than decorative fills.
- [ ] Remove loud fintech green and blue treatments from routine success and action states.

### 10. Tighten Copy And Information Architecture

- [ ] Replace aspirational or homepage-style copy with utility copy across all authenticated routes.
- [ ] Ensure every page answers where am I, what changed, and what should I do next within the first screen.
- [ ] Rename headings and labels so they read like operational UI, not marketing or dashboard cliches.
- [ ] Keep section descriptions to one sentence whenever possible.
- [ ] Ensure scanning headings, values, table labels, and exceptions is enough to understand each page.
- [ ] Keep rules, plugin mappings, and secondary administration surfaces visually subordinate to the five core screens.

### 11. Final Visual And Interaction QA

- [ ] Verify the shell reads as private, serious, and domestic rather than fintech-generic or enterprise-corporate.
- [ ] Verify verdigris is the only brand accent and semantic tones appear only when data meaning requires them.
- [ ] Verify the design still feels premium if most shadows are removed.
- [ ] Verify no core screen defaults to card mosaics, KPI tile farms, or homepage hero composition.
- [ ] Verify dashboard, accounts, import, budgets, and forecast each have one dominant visual idea and one clear primary plane.
- [ ] Verify drawers, dialogs, and inspectors preserve spatial orientation on desktop and mobile.
- [ ] Verify tables remain dense, readable, and calm on large and narrow screens.
- [ ] Verify reduced-motion mode still communicates hierarchy and updates clearly.
- [ ] Verify all five core screens feel like rooms inside one system, not five separate micro-designs.

## Completion Criteria

- [x] The first screen after sign-in is an operational dashboard, not a landing page.
- [ ] The app shell uses the visual thesis consistently across navigation, typography, color, surface, and density.
- [ ] Dashboard, accounts, import, budgets, and forecast all match the content plan structurally.
- [ ] Motion and state feedback match the interaction thesis behaviorally.
- [ ] The rewritten UI feels like a private household finance instrument rather than a generic SaaS app.
