# Prospero — Personal Finance Tracker

## Feature Specification

> **Version:** 0.1.0-draft
> **Last Updated:** 2026-02-27
> **Status:** Planning

---

## 1. Vision & Goals

Prospero is an **offline-first, self-hosted personal finance tracker** built on .NET Aspire. It provides comprehensive financial asset tracking, a user-configurable ingestion pipeline, intelligent transaction categorization, budgeting, forecasting, and investment portfolio tracking — all without cloud dependencies.

### Core Principles

| Principle | Description |
|-----------|-------------|
| **Offline-First** | Full functionality via Blazor WASM PWA with local storage; syncs when server is reachable |
| **Self-Hosted** | Runs entirely on user's own infrastructure via Docker Compose; no external cloud services required |
| **Privacy & Security** | Encryption at rest and in transit; local authentication fallback; no data leaves the network |
| **Multi-User** | Household/family support with shared and individual accounts |
| **Extensible** | Plugin architecture for importers, price feeds, and rules |
| **Country-Agnostic** | Starts with Canadian financial concepts (RRSP, TFSA) but architected for any locale |

---

## 2. Asset & Account Types

### 2.1 Investment Accounts

| Feature | Description | Priority |
|---------|-------------|----------|
| **RRSP Tracking** | Balance, holdings, contribution room, lot-level ACB tracking | P0 |
| **TFSA Tracking** | Balance, holdings, contribution room, lot-level tracking | P0 |
| **Non-Registered Accounts** | Holdings with adjusted cost base (ACB) for capital gains | P0 |
| **RESP / LIRA / Other** | Extensible account types via configuration | P1 |
| **Holdings-Level Detail** | Individual stocks, ETFs, mutual funds with quantities & book values | P0 |
| **Lot-Level Tracking** | Per-purchase lots for ACB calculation (FIFO, ACB average, specific ID) | P1 |
| **Automatic Price Updates** | Fetch market prices via plugin architecture (Yahoo Finance, etc.) | P1 |
| **Performance Calculation** | Time-weighted & money-weighted returns per account/holding | P1 |
| **Dividend Tracking** | Track dividend income, DRIP reinvestments | P1 |

### 2.2 Banking Accounts

| Feature | Description | Priority |
|---------|-------------|----------|
| **Chequing Accounts** | Full transaction tracking with categorization | P0 |
| **Savings Accounts** | Balance and interest tracking | P0 |
| **Joint Accounts** | Shared between household members | P1 |

### 2.3 Credit & Debt

| Feature | Description | Priority |
|---------|-------------|----------|
| **Credit Cards** | Transaction-level tracking, statement periods, minimum payments | P0 |
| **Mortgages** | Balance tracking, amortization schedules, payment tracking | P0 |
| **Lines of Credit** | Balance and interest tracking | P1 |
| **Loans** | Auto loans, student loans, personal loans with amortization | P1 |

### 2.4 Property & Assets

| Feature | Description | Priority |
|---------|-------------|----------|
| **Real Estate** | Estimated value tracking (manual or automated) | P0 |
| **Property Expenses** | Taxes, insurance, maintenance, utilities | P1 |
| **Vehicles** | Value depreciation tracking | P2 |
| **Other Assets** | User-defined asset categories | P2 |

---

## 3. Data Ingestion Pipeline

### 3.1 File Import

| Feature | Description | Priority |
|---------|-------------|----------|
| **CSV Import** | Upload and parse CSV files with configurable column mapping | P0 |
| **QFX/OFX Import** | Parse Open Financial Exchange format files | P0 |
| **QIF Import** | Parse Quicken Interchange Format files | P0 |
| **Column Mapping UI** | Drag-and-drop or dropdown column-to-field mapping interface | P0 |
| **Saved Mappings** | Save and reuse column mappings per institution/file format | P0 |
| **Preview & Validation** | Preview parsed transactions before committing import | P0 |
| **Duplicate Detection** | Detect and flag potential duplicate transactions during import | P0 |
| **Idempotent Import Keys** | Use stable transaction fingerprints (`institution + account + posted date + amount + normalized memo + external reference`) to prevent duplicate inserts across repeated imports | P0 |
| **Import Diagnostics** | Row-level error reporting with actionable reasons (parse error, missing mapping, invalid date/amount, duplicate) and downloadable error CSV | P1 |
| **Partial Import Policy** | Configurable behavior: fail-fast, skip-invalid-and-continue, or preview-only with no commit | P1 |
| **Import History** | Track all imports with ability to undo/rollback | P1 |

### 3.2 Manual Entry

| Feature | Description | Priority |
|---------|-------------|----------|
| **Quick-Add Transaction** | Fast single transaction entry with autocomplete | P0 |
| **Batch Entry** | Enter multiple transactions at once | P1 |
| **Transfer Between Accounts** | Create linked transfer transactions | P0 |
| **Split Transactions** | Logical split: one parent transaction with child split lines, each assigned a category, amount, and optional tags. The parent retains the original amount and source data; child splits must sum to the parent. Splits are first-class in reporting, budgets, and rules. | P0 |

### 3.3 Rules Engine

| Feature | Description | Priority |
|---------|-------------|----------|
| **Rule Builder UI** | Visual rule creation: IF condition(s) THEN action(s) | P0 |
| **Condition Types** | Match on: description contains/regex, amount range, date range, payee, account | P0 |
| **Action Types** | Set: category, tags, payee name (normalize), notes. Auto-split: rules can define split templates (e.g., "Costco → 80% Groceries / 20% Household") applied as logical child splits. | P0 |
| **Rule Priority/Ordering** | User-defined execution order with drag-and-drop | P0 |
| **Auto-Apply on Import** | Rules execute automatically during file import | P0 |
| **Batch Re-Apply** | Re-run rules against existing transactions | P1 |
| **Rule Suggestions** | System suggests rules based on user patterns | P1 |
| **Rules Library** | Use a proven .NET rules engine library (e.g., RulesEngine by Microsoft, NRules) | P0 |

### 3.4 AI-Assisted Categorization (Ollama Integration)

| Feature | Description | Priority |
|---------|-------------|----------|
| **Ollama Integration** | Connect to a local Ollama instance for LLM inference | P1 |
| **Transaction Categorization** | LLM suggests categories for uncategorized transactions | P1 |
| **Payee Normalization** | LLM normalizes messy payee names (e.g., "AMZN*1A2B3C" → "Amazon") | P1 |
| **Learning from User** | Model improves suggestions based on user corrections | P2 |
| **Bulk Categorization** | AI processes batches of uncategorized transactions | P1 |
| **Confidence Scoring** | Show confidence level; auto-apply above threshold, flag for review below | P1 |

### 3.5 Plugin Architecture

| Feature | Description | Priority |
|---------|-------------|----------|
| **Importer Plugin Interface** | `ITransactionImporter` interface for custom file format parsers | P1 |
| **Price Feed Plugin Interface** | `IPriceFeedProvider` interface for market data sources | P1 |
| **Plugin Discovery** | Automatic discovery of plugins from a designated directory | P1 |
| **Plugin Configuration** | Per-plugin settings via the UI | P1 |
| **Built-in Plugins** | CSV, QFX/QIF, Yahoo Finance price feed as reference implementations | P1 |

### 3.6 Import Quality Controls

| Feature | Description | Priority |
|---------|-------------|----------|
| **Dead-Letter Import Rows** | Invalid rows are retained in an import exception queue for correction and replay | P1 |
| **Import Replay** | Re-run a prior import batch after mapping/rule corrections while preserving auditability | P1 |
| **Import Quality Metrics** | Track import success rate, duplicate rate, auto-categorization rate, and manual fix rate | P1 |

---

## 4. Budgeting & Forecasting

### 4.1 Budgets

| Feature | Description | Priority |
|---------|-------------|----------|
| **Monthly Budgets** | Set budget amounts per category per month | P0 |
| **Budget Templates** | Create templates and apply to future months | P1 |
| **Rollover Budgets** | Unspent amounts roll to next month (configurable) | P1 |
| **Sinking Funds** | Save toward lump-sum expenses (e.g., annual insurance, property tax). Define the target amount and due date; the system calculates the required monthly set-aside and tracks accumulated vs target. Sinking fund balances are virtual — they don't require a separate account. Alerts when underfunded or due date approaching. | P0 |
| **Budget vs Actual** | Visual comparison of budgeted vs actual spending, including sinking fund progress bars | P0 |
| **Budget Alerts** | Notifications when approaching or exceeding budget limits, or when sinking fund contributions are behind schedule | P1 |
| **Category Groups** | Group categories for higher-level budget views | P0 |
| **Income Budgeting** | Track expected vs actual income | P1 |
| **Split-Aware Budgets** | Budget tracking uses split line categories, not the parent transaction category, ensuring accurate per-category spend tracking | P0 |

### 4.2 Recurring Transactions

| Feature | Description | Priority |
|---------|-------------|----------|
| **Recurring Definitions** | Define recurring income/expense patterns (weekly, biweekly, monthly, etc.) | P0 |
| **Auto-Generation** | Automatically generate expected future transactions | P0 |
| **Match to Actual** | Match recurring expectations to actual transactions when imported | P1 |
| **Variance Alerts** | Alert when actual amount deviates from expected | P1 |
| **Skip / Adjust** | Skip individual occurrences or adjust amounts | P0 |

### 4.3 Forecasting

| Feature | Description | Priority |
|---------|-------------|----------|
| **Cash Flow Forecast** | Project account balances based on recurring transactions, budgets, and sinking fund contributions | P0 |
| **Forecast Horizon** | Configurable: 1 month, 3 months, 6 months, 1 year, 5 years, 10 years | P0 |
| **Growth Rate Assumptions** | Set per-account or per-asset-class annual growth rates for investment accounts and property appreciation | P0 |
| **Named Scenarios** | Create and save named forecast scenarios (e.g., "Conservative", "Moderate", "Aggressive") with different growth rates, contribution levels, and payment schedules | P1 |
| **What-If Scenarios** | Model changes (e.g., "what if I increase mortgage payments by $200?" or "what if markets return 4% instead of 7%?") | P1 |
| **Minimum Balance Alerts** | Warn if projected balance drops below threshold | P1 |
| **Sinking Fund Forecasting** | Project sinking fund accumulation and show when funds will be ready vs when payment is due | P0 |

---

## 5. Dashboards & Reporting

### 5.1 Net Worth Dashboard

| Feature | Description | Priority |
|---------|-------------|----------|
| **Net Worth Summary** | Total assets minus total liabilities | P0 |
| **Net Worth Over Time** | Line chart showing net worth history | P0 |
| **Asset Allocation** | Pie/donut chart of asset categories | P0 |
| **Account Summary Cards** | Quick-view cards for each account with balance and trend | P0 |
| **Net Worth Forecast** | Project future net worth based on scheduled payments (recurring transactions, mortgage amortization, sinking funds) and user-defined growth rate assumptions per account/asset class | P0 |
| **Growth Rate Scenarios** | Define multiple forecast scenarios (conservative / moderate / aggressive) with different growth rates per asset class. Compare scenarios side-by-side on the same chart. | P1 |
| **Forecast Inputs** | Forecasting accounts for: recurring contributions/withdrawals, scheduled debt payments, investment growth rates, property appreciation rates, and inflation adjustment | P1 |

### 5.2 Spending Analysis

| Feature | Description | Priority |
|---------|-------------|----------|
| **Spending by Category** | Bar/pie charts for any date range | P0 |
| **Spending Trends** | Month-over-month spending trends per category | P0 |
| **Top Payees** | Ranked list of where money goes | P1 |
| **Category Drill-Down** | Click a category to see transactions | P0 |

### 5.3 Investment Performance

| Feature | Description | Priority |
|---------|-------------|----------|
| **Portfolio Overview** | Total portfolio value, gain/loss, allocation | P1 |
| **Per-Holding Performance** | Individual holding returns, gain/loss | P1 |
| **Sector/Asset Allocation** | Breakdown by sector, asset class, geography | P2 |
| **Benchmark Comparison** | Compare portfolio returns against a benchmark index | P2 |

### 5.4 Cash Flow

| Feature | Description | Priority |
|---------|-------------|----------|
| **Income vs Expenses** | Monthly income vs expense comparison | P0 |
| **Cash Flow Waterfall** | Waterfall chart showing inflows and outflows | P1 |
| **Savings Rate** | Track savings rate over time | P1 |

### 5.5 Debt Tracking

| Feature | Description | Priority |
|---------|-------------|----------|
| **Debt Overview** | Total debt, interest rates, minimum payments | P0 |
| **Amortization Charts** | Visual mortgage/loan amortization | P1 |
| **Debt Payoff Projections** | Snowball/avalanche payoff strategies | P2 |

---

## 6. Authentication & Multi-User

### 6.1 Authentication

| Feature | Description | Priority |
|---------|-------------|----------|
| **Local Username/Password** | Built-in identity for offline-only deployments | P0 |
| **OpenID Connect** | SSO via Google, Microsoft, or other OIDC providers | P0 |
| **Two-Factor Auth (TOTP)** | Optional 2FA via authenticator app | P1 |
| **Session Management** | Configurable session timeout, concurrent session limits | P1 |
| **Password Policy** | Configurable complexity requirements | P0 |

### 6.2 Multi-User & Households

| Feature | Description | Priority |
|---------|-------------|----------|
| **User Profiles** | Individual user accounts with preferences | P0 |
| **Household Groups** | Link users into a household for shared finances | P1 |
| **Shared Accounts** | Mark accounts as visible to all household members | P1 |
| **Private Accounts** | Keep certain accounts visible only to the owner | P1 |
| **Role-Based Access** | Admin (manage users, settings) vs Member (view/edit own data) | P1 |
| **Permissions Matrix** | Resource-level permissions for `Account`, `Transaction`, `ImportBatch`, `Rule`, and `Report` at user/household scope | P0 |

---

## 7. Security & Data Protection

| Feature | Description | Priority |
|---------|-------------|----------|
| **HTTPS Everywhere** | TLS for all communications; auto-redirect HTTP → HTTPS | P0 |
| **Encryption at Rest** | Database-level encryption; sensitive fields (account numbers) encrypted at application level | P0 |
| **CSRF / XSS Protection** | Blazor's built-in protections plus CSP headers | P0 |
| **Audit Logging** | Track data changes (who, what, when) | P1 |
| **Rate Limiting** | Protect API endpoints from abuse | P1 |
| **Input Validation** | Server-side validation on all inputs using FluentValidation | P0 |
| **Key Management Lifecycle** | Encryption key generation, rotation schedule, secure backup, restore validation, and compromised-key recovery runbook | P0 |

---

## 8. Data Management

| Feature | Description | Priority |
|---------|-------------|----------|
| **Automated Backups** | Scheduled PostgreSQL backups with retention policy | P1 |
| **Manual Backup** | On-demand backup download | P0 |
| **Data Export** | Export all data as CSV, JSON, or OFX | P0 |
| **Data Import (Restore)** | Restore from backup | P1 |
| **Data Portability** | Complete data export in open format | P0 |
| **Restore Verification** | Scheduled test restore job with checksum validation and smoke test to verify backup integrity | P0 |
| **Recovery Objectives** | Configurable and documented RPO/RTO targets for self-hosted operators | P1 |

---

## 9. Offline-First / PWA

| Feature | Description | Priority |
|---------|-------------|----------|
| **Service Worker** | Cache app shell and static assets for offline access | P0 |
| **IndexedDB Local Store** | Store recent transactions and account summaries locally | P0 |
| **Background Sync** | Queue changes made offline; sync when server is reachable | P0 |
| **Conflict Resolution** | Field-aware merge policies for financial records (money/splits/categories are conflict-sensitive) plus explicit user conflict queue; avoid blind last-write-wins for critical fields | P0 |
| **Sync Idempotency** | Client-generated operation IDs guarantee exactly-once logical application of offline mutations | P0 |
| **Offline Indicators** | Clear UI indicators for online/offline status and pending sync | P0 |
| **Install Prompt** | PWA install prompt for desktop and mobile | P0 |

---

## 10. Internationalization & Localization

| Feature | Description | Priority |
|---------|-------------|----------|
| **Multi-Currency** | Support multiple currencies with configurable base currency | P0 |
| **Exchange Rates** | Fetch exchange rates via plugin (or manual entry) | P1 |
| **Locale-Aware Formatting** | Date, number, and currency formatting based on user locale | P0 |
| **Extensible Account Types** | Account types not hardcoded to Canadian system | P0 |
| **Language Support** | English initially; architecture supports future i18n | P2 |

---

## 11. Deployment & Operations

| Feature | Description | Priority |
|---------|-------------|----------|
| **Docker Compose** | Single `docker compose up` to run the full stack | P0 |
| **.NET Aspire Orchestration** | Aspire AppHost for development with Dashboard | P0 |
| **Dev Mode** | `dotnet run` with hot reload, Aspire dashboard, seeded data | P0 |
| **Health Checks** | Liveness and readiness probes for all services | P0 |
| **Structured Logging** | Serilog with OpenTelemetry export | P0 |
| **Configuration** | Environment-based configuration with sensible defaults | P0 |
| **Automated Migrations** | EF Core migrations applied on startup | P0 |
| **Container Registry** | GitHub Container Registry (ghcr.io) for published images | P1 |
| **CI/CD** | GitHub Actions for build, test, publish images | P1 |

---

## 12. Reconciliation & Data Integrity

### 12.1 Reconciliation Workflow

| Feature | Description | Priority |
|---------|-------------|----------|
| **Statement Reconciliation** | Reconcile accounts by statement period (opening/closing balance, cleared vs uncleared transactions) | P0 |
| **Reconciliation Report** | Show mismatches, missing transactions, and balance delta with exportable report | P1 |
| **Statement Close Lock** | Optional period lock after reconciliation to prevent accidental edits without explicit unlock/audit reason | P1 |

### 12.2 Financial Invariants

| Feature | Description | Priority |
|---------|-------------|----------|
| **Split Sum Invariant** | Enforce exact equality of parent amount and sum of split amounts in minor units after rounding | P0 |
| **Currency Invariant** | Parent and split currency consistency, or explicit FX conversion records when different | P0 |
| **Transfer Invariant** | Inter-account transfer creates balanced linked entries with immutable linkage IDs | P0 |
| **Audit Invariant** | Every mutation of transactions/splits/rules/import batches produces an auditable event trail | P0 |

---

## Priority Legend

| Priority | Meaning | Phase Target |
|----------|---------|-------------|
| **P0** | Must-have for MVP and safe production operation | Phase 1–4 |
| **P1** | Important enhancements after stable MVP | Phase 5–6 |
| **P2** | Nice-to-have, future | Post Phase 6 |
