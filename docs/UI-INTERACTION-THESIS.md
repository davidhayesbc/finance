# Privestio UI Interaction Thesis

## Purpose

This document defines how Privestio should move, reveal, respond, and transition across the five core screens:

- dashboard
- accounts
- import
- budgets
- forecast

It extends [UI-VISUAL-THESIS.md](UI-VISUAL-THESIS.md) and [UI-CONTENT-PLAN.md](UI-CONTENT-PLAN.md) by describing the behavioral layer of the product.

The goal is not animation for its own sake. The goal is to make the product feel exact, calm, and spatially coherent.

## Interaction Thesis

Privestio should behave like a disciplined instrument panel: surfaces settle into place with measured weight, data changes register with quiet clarity, and every transition helps the user preserve orientation.

## Core Principles

### Motion Should Confirm Structure

Motion exists to answer:

1. what changed
2. where it changed
3. how important it is

If a transition does not improve orientation, hierarchy, or affordance, it should be removed.

### Behavior Should Feel Weighted

Nothing should snap in like a consumer app or bounce like a playful dashboard. Surfaces should feel lightly weighted, as though they are sliding on tracks rather than floating in empty space.

### Data Changes Should Read Before They Decorate

When balances, forecasts, errors, or status indicators update, the user should understand the new state immediately without waiting for ornamental motion to complete.

### The Product Should Prefer Continuity Over Surprise

Users will spend time comparing values, tracing rows, and monitoring changes. Preserve context whenever possible.

- filters refine in place
- tables maintain scroll position when practical
- drawers keep the underlying page visible
- range changes morph charts rather than replace them abruptly

## Primary Motion Ideas

These are the defining interaction moves for the refreshed product.

### 1. Settling Entrance

Each page should load in a short sequence:

- orientation band appears first
- primary working plane settles next
- supporting evidence fades in last

This should feel like structural assembly, not staged theater.

Behavior:

- short upward settle or slight de-blur on entry
- strongest emphasis on the primary plane
- secondary regions enter with less distance and lower opacity change

Purpose:

- establish hierarchy within 300 to 450 ms
- give each screen one immediate focal point

### 2. Data Recomposition

When date ranges, forecast horizons, filters, or grouping modes change, the page should recompose rather than hard-refresh.

Behavior:

- chart lines morph or redraw from current geometry
- table rows reorder with continuity where feasible
- summary values count or crossfade quickly rather than blinking

Purpose:

- preserve the user's mental map of the page
- make analytical comparison feel trustworthy

### 3. Inspector Depth

Editing, drill-in, and exception review should usually happen in a side sheet, drawer, or anchored panel rather than a disruptive full-screen replacement.

Behavior:

- the base page dims slightly but remains legible
- the overlay slides in on a firm track with minimal overshoot
- closing the surface returns focus to the invoking element

Purpose:

- keep the product feeling like one continuous workspace
- support quick review and correction loops

## Motion Vocabulary

Use a restrained vocabulary across the product.

### Allowed

- fade
- slight translate on entry or exit
- scale from 0.985 to 1 for dialogs or inspector surfaces
- height expansion for inline disclosure
- crossfade for values and small status transitions
- shared-axis chart redraws

### Avoid

- bounce
- springy overshoot for core data surfaces
- parallax in routine product pages
- spinning counters or celebratory motion
- cascading tile reveals across large data grids
- attention-seeking shimmer on settled UI

## Timing Model

Use consistent duration ranges.

- Micro feedback: 80 to 140 ms
- Inline reveal or disclosure: 140 to 220 ms
- Page entrance: 220 to 360 ms
- Drawer or side sheet: 220 to 320 ms
- Dialog presence: 160 to 240 ms
- Chart recomposition: 220 to 420 ms depending on data density

General rules:

- shorter for routine actions
- slightly longer for large structural surfaces
- never let motion delay reading of critical balances or errors

## Easing Model

Default toward quiet, decisive easing.

- enter: ease-out with light deceleration
- exit: ease-in, slightly faster than enter
- data recomposition: ease-in-out, but subtle

The product should feel composed, not elastic.

## Page Transitions

### Cross-Screen Navigation

Top-level page changes should keep the navigation chrome stable while the content area changes.

Behavior:

- navigation rail and app frame stay fixed
- outgoing page content fades down slightly
- incoming orientation band appears first, then the primary plane

Purpose:

- reinforce that pages are rooms inside one system
- reduce the sensation of full reloads

### Same-Context Navigation

When moving from accounts to account detail, or from a list to a focused view, the transition should feel like stepping deeper into the same dossier.

Behavior:

- preserve background color and frame continuity
- let the destination header arrive from the same vertical rhythm as the source list
- if row-linked transitions are feasible later, the selected row should visually anchor the destination

### Refresh Behavior

Refresh should almost never look like a page reset.

Use:

- localized skeletons
- dim-and-refresh patterns on charts and tables
- inline progress states on buttons and selectors

Avoid:

- clearing the whole screen to a spinner when only one region is updating

## Chart Reveal Behavior

Charts are major narrative surfaces in this product. They should feel authoritative.

### Initial Load

- axes and structural frame appear first
- data line, area, or bars draw in once structure is visible
- labels and tooltips should not animate heavily

### Range Changes

- preserve the chart frame
- morph the data path where feasible
- if the scale changes dramatically, crossfade the series while keeping axis continuity

### Hover and Focus

- use a thin rule, dot, or highlight band
- reveal values with a compact tooltip or value rail
- reduce surrounding series contrast slightly instead of over-highlighting the active point

### Empty State

- empty states should settle in with the same calm timing as data
- explain what is missing and what action will produce data

### Dashboard and Forecast Guidance

- dashboard net-worth motion should privilege continuity over flourish
- forecast projection should emphasize future trajectory and flagged low points
- avoid animated donut spins, pulsing thresholds, or decorative chart gradients

## Table and Filter Interactions

Tables are the core operational surface for accounts, budgets, and import diagnostics.

### Table Behavior

- row hover should be quiet and structural, not glowing
- sorting should preserve header position and animate row reordering subtly where possible
- selected or active rows should rely on tone and edge treatment, not thick borders

### Density

- maintain dense rows with clean line height
- do not expand rows dramatically on hover
- action controls should appear as a refinement of the row, not a takeover of it

### Filters

- opening a filter surface should feel like exposing a control layer, not launching a modal event
- applied filters should update the primary plane in place
- active filters should remain visible in a compact strip or header summary

### Search

- searching should debounce lightly and update results in place
- highlight matched text sparingly
- do not animate every row independently on large result sets

### Loading and Empty Conditions

- table loading should use line-based skeletons that preserve column structure
- empty results after filtering should read as a filter outcome, not as missing data

## Exception-State Emphasis

The product will surface many kinds of risk and inconsistency: stale data, budget pressure, failed imports, negative projections, duplicate transactions.

These states need emphasis without emotional noise.

### Severity Model

- Informational: neutral emphasis, often inline
- Advisory: warm accent or muted forecast accent
- Urgent: risk color with sharp contrast and compact language

### Presentation Rules

- highlight the affected number, row, or period before adding banners
- prefer local emphasis near the source of the issue
- reserve page-level message bars for workflow-blocking or broad system states

### Motion Rules For Exceptions

- use one-time reveal or controlled pulse only when new urgent information appears
- never loop attention animations on financial data surfaces
- if a threshold is crossed, transition the tone immediately and clearly

### Recommended Patterns

- dashboard watchlist rows fade in when new issues appear
- over-budget categories gain a sharper badge and more assertive progress-track contrast
- forecast risk months gain a marker, rule, or dot treatment rather than a loud flashing state
- import errors expand inline from the affected result set

## Drawers, Dialogs, and Side Sheets

Privestio should prefer layered editing over full interruption.

### Side Sheets

Use for:

- budget create or edit
- account quick detail
- transaction detail review
- mapping details
- assumption editing in forecast

Behavior:

- enter from the right on desktop
- enter from the bottom as a full-height sheet on narrow screens
- retain the underlying page as context
- keep the header concise and action-led

### Dialogs

Use for:

- destructive confirmation
- short focused forms
- single-purpose decisions with limited context

Behavior:

- scale and fade in quickly
- keep background dimming light
- dismiss cleanly with escape and explicit close control where appropriate

### Inline Expansion

Use for:

- advanced settings
- row details
- validation help

Behavior:

- expand vertically from the trigger zone
- avoid pushing the whole page abruptly if large content can live in a sheet instead

## Screen-Specific Interaction Notes

## Dashboard

### Behavioral Priorities

- the net-worth plane should settle first after the header
- posture-strip values should crossfade or count in quickly
- watchlist items should appear as structured updates, not as notification toasts

### Key Interactions

- range selector updates chart and posture values in place
- account summary rows support direct drill-in with minimal friction
- notification access should feel adjacent to the watchlist, not detached from it

### Motion Character

Quiet and confident. This screen should feel like arrival.

## Accounts

### Behavioral Priorities

- the ledger table is the anchor
- filtering and sorting must preserve context
- historical sync should update affected regions without resetting the whole page

### Key Interactions

- range control updates the history strip only
- account row actions open detail progressively
- stale-data states should sit directly in the table or summary support zone

### Motion Character

Rigid and architectural. Very little flourish.

## Import

### Behavioral Priorities

- the current step must remain obvious
- validation states must appear immediately next to the affected control or row set
- commit should feel deliberate and irreversible, not dramatic

### Key Interactions

- moving between steps should slide the workspace laterally or crossfade within a stable frame
- sample rows and mappings should remain aligned while the user edits field assignment
- preview counts should update with fast, controlled numeric changes

### Motion Character

Procedural and precise. The user should feel guided, not entertained.

## Budgets

### Behavioral Priorities

- month changes should recompose the category table and summary band in place
- over-budget states should sharpen instantly when thresholds are crossed
- editing a budget should open in a side sheet or anchored editing band

### Key Interactions

- progress indicators update without flourish
- risky categories can pin to an at-risk list or sort to the top on demand
- inline category drill-down should feel like opening a ledger seam, not leaving the page

### Motion Character

Measured and diagnostic. This screen should feel like review, not coaching.

## Forecast

### Behavioral Priorities

- horizon changes should recompose the projection rather than replace it
- risk months should become legible through markers and localized emphasis
- assumptions should open in a layered panel, keeping the main projection visible

### Key Interactions

- selecting a period in the ledger highlights the corresponding point on the projection
- changing scenario updates summary, chart, and driver content with shared timing
- minimum balance thresholds should reveal as rules or markers, not banners by default

### Motion Character

Analytical and slightly tense. Enough movement to express future pressure, never enough to feel theatrical.

## State Feedback

### Success

Success should feel controlled.

- use short confirmation messages
- update the affected surface immediately
- avoid celebratory animation, confetti, or oversized color floods

### Pending

Pending states should live on the control or region causing the wait.

- buttons show progress where they were invoked
- charts dim slightly during recomposition
- tables preserve headers and row structure while loading

### Error

Errors should be local when possible.

- field-level validation next to the field
- row-level import errors in the result area
- page-level bars only for broad failures or blocked workflows

## Accessibility and Reduced Motion

Motion must remain optional and never be required for understanding.

- respect reduced-motion preferences by shortening or removing non-essential transitions
- preserve focus clearly after every drawer, dialog, and step change
- never communicate status by color alone
- ensure hover-only affordances also exist for keyboard and touch

When reduced motion is enabled:

- replace movement with quick fades or instant state changes
- keep hierarchy through contrast and layout rather than animation distance

## Interaction Litmus Test

The interaction layer is successful if a user can:

1. change a range, filter, or scenario without losing orientation
2. spot urgent financial issues without being visually shouted at
3. open details, make an edit, and return to the main workspace without feeling displaced
4. understand what is updating and why during sync, import, and recalculation states

## Next Design Step

Use this document to produce one high-fidelity screen concept with explicit interaction notes, starting with either:

1. dashboard
2. accounts
