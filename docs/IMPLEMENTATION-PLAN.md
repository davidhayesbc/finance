# Prospero — Implementation Plan

> **Version:** 0.1.0-draft
> **Last Updated:** 2026-02-27
> **Status:** Planning

---

## Table of Contents

1. [Technology Stack](#1-technology-stack)
2. [Architecture Overview](#2-architecture-overview)
3. [Solution Structure](#3-solution-structure)
4. [Domain Model](#4-domain-model)
5. [Phased Implementation](#5-phased-implementation)
6. [Infrastructure & DevOps](#6-infrastructure--devops)

---

## 1. Technology Stack

### Runtime & Framework

| Component | Technology | Rationale |
|-----------|-----------|-----------|
| **Runtime** | .NET 10 | Latest LTS, top performance, native AOT potential |
| **Orchestration** | .NET Aspire 9.x | Service discovery, health checks, local dev dashboard, OpenTelemetry |
| **API** | ASP.NET Core Minimal APIs | Low ceremony, high performance, good for resource-oriented design |
| **Frontend** | Blazor WebAssembly (PWA) | Offline-first capable, C# full-stack, PWA installable |
| **ORM** | Entity Framework Core 10 | Migrations, LINQ queries, PostgreSQL provider |
| **Database** | PostgreSQL 17 | Robust, financial-grade, excellent .NET Aspire integration |

### Key Libraries

| Library | Purpose |
|---------|---------|
| **Microsoft.RulesEngine** | User-configurable business rules for transaction categorization |
| **FluentValidation** | Input validation across API and domain layers |
| **Serilog + OpenTelemetry** | Structured logging with distributed tracing |
| **OFXSharp / QIF parser** | OFX/QFX and QIF file format parsing |
| **CsvHelper** | CSV parsing with configurable column mapping |
| **Microsoft.AspNetCore.Identity** | Local username/password authentication |
| **Microsoft.AspNetCore.Authentication.OpenIdConnect** | Google/Microsoft SSO |
| **Blazored.LocalStorage** | IndexedDB/localStorage access from Blazor WASM |
| **MudBlazor** or **Fluent UI Blazor** | Component library for the PWA UI |
| **Microsoft.AspNetCore.DataProtection** | Application-level field encryption |
| **Yarp** or Aspire built-in | Reverse proxy if needed |
| **OllamaSharp** | .NET client for local Ollama LLM integration |

### Infrastructure

| Component | Technology |
|-----------|-----------|
| **Containers** | Docker with multi-stage builds |
| **Orchestration (Dev)** | .NET Aspire AppHost |
| **Orchestration (Prod)** | Docker Compose |
| **CI/CD** | GitHub Actions |
| **Container Registry** | GitHub Container Registry (ghcr.io) |
| **Reverse Proxy / TLS** | Caddy or Traefik (auto-HTTPS in Docker Compose) |

---

## 2. Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Docker Compose                           │
│                                                                 │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────┐   │
│  │   Caddy /    │   │  Prospero    │   │    PostgreSQL     │   │
│  │   Traefik    │──▶│  API Server  │──▶│    (encrypted)    │   │
│  │  (TLS/HTTPS) │   │  (ASP.NET)   │   │                  │   │
│  └──────┬───────┘   └──────┬───────┘   └──────────────────┘   │
│         │                  │                                    │
│         │           ┌──────┴───────┐   ┌──────────────────┐   │
│         │           │   Aspire     │   │     Ollama       │   │
│         │           │  Dashboard   │   │   (optional)     │   │
│         │           │  (dev only)  │   │                  │   │
│         │           └──────────────┘   └──────────────────┘   │
│         │                                                      │
│  ┌──────┴───────────────────────────────────────────────────┐  │
│  │              Static Files (Blazor WASM)                   │  │
│  │              Served by API Server or Caddy                │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      Client (Browser)                           │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Blazor WASM PWA                              │  │
│  │  ┌──────────┐  ┌──────────┐  ┌────────────────────┐     │  │
│  │  │ Service  │  │ IndexedDB│  │  Sync Engine       │     │  │
│  │  │ Worker   │  │ (offline │  │  (queue changes,   │     │  │
│  │  │ (cache)  │  │  store)  │  │   resolve conflicts)│     │  │
│  │  └──────────┘  └──────────┘  └────────────────────┘     │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Backend Layered Architecture

```
┌──────────────────────────────────────────┐
│              API Layer                    │  Minimal API endpoints
│          (Presentation)                  │  DTOs, Validation, Auth
├──────────────────────────────────────────┤
│           Application Layer              │  Use cases, orchestration
│         (CQRS with MediatR)             │  Commands, Queries, Handlers
├──────────────────────────────────────────┤
│            Domain Layer                  │  Entities, Value Objects
│         (Pure C#, no deps)              │  Domain Events, Rules
├──────────────────────────────────────────┤
│         Infrastructure Layer             │  EF Core, File parsing
│       (Data Access, Plugins)            │  External integrations
└──────────────────────────────────────────┘
```

### Offline-First Data Flow

```
User Action (offline)
    │
    ▼
┌──────────────┐     ┌──────────────┐
│  Blazor WASM │────▶│  IndexedDB   │  (write to local store)
│  Component   │     │  Local Store │
└──────────────┘     └──────┬───────┘
                            │
                     ┌──────▼───────┐
                     │  Sync Queue  │  (track pending changes)
                     └──────┬───────┘
                            │  (when online)
                     ┌──────▼───────┐
                     │  Sync Engine │──▶ POST /api/sync
                     └──────┬───────┘
                            │
                     ┌──────▼───────┐
                     │  Server API  │──▶ PostgreSQL
                     └──────┬───────┘
                            │
                     ┌──────▼───────┐
                     │  Sync        │  (return server changes)
                     │  Response    │──▶ Update IndexedDB
                     └──────────────┘
```

---

## 3. Solution Structure

```
finance/
├── docs/
│   ├── FEATURES.md
│   └── IMPLEMENTATION-PLAN.md
│
├── src/
│   ├── Prospero.AppHost/                    # .NET Aspire orchestrator
│   │   ├── Program.cs
│   │   └── Prospero.AppHost.csproj
│   │
│   ├── Prospero.ServiceDefaults/            # Shared Aspire service defaults
│   │   ├── Extensions.cs
│   │   └── Prospero.ServiceDefaults.csproj
│   │
│   ├── Prospero.Domain/                     # Domain layer (pure C#, no dependencies)
│   │   ├── Entities/
│   │   │   ├── Account.cs                   # Base account entity
│   │   │   ├── Transaction.cs
│   │   │   ├── Budget.cs
│   │   │   ├── RecurringTransaction.cs
│   │   │   ├── Holding.cs
│   │   │   ├── Lot.cs
│   │   │   └── User.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs                     # Currency + amount value object
│   │   │   ├── DateRange.cs
│   │   │   ├── AccountType.cs               # Smart enum (Investment, Banking, Credit, Property)
│   │   │   └── TransactionCategory.cs
│   │   ├── Enums/
│   │   │   ├── AccountSubType.cs            # RRSP, TFSA, Chequing, etc.
│   │   │   ├── TransactionType.cs           # Debit, Credit, Transfer
│   │   │   └── CostBasisMethod.cs           # FIFO, AverageCost, SpecificLot
│   │   ├── Events/
│   │   │   ├── TransactionCreated.cs
│   │   │   ├── AccountBalanceChanged.cs
│   │   │   └── ImportCompleted.cs
│   │   ├── Interfaces/
│   │   │   ├── IEntity.cs
│   │   │   ├── IAuditableEntity.cs
│   │   │   ├── ITransactionImporter.cs      # Plugin interface
│   │   │   ├── IPriceFeedProvider.cs         # Plugin interface
│   │   │   └── IRuleEvaluator.cs
│   │   └── Prospero.Domain.csproj
│   │
│   ├── Prospero.Application/                # Application layer (use cases)
│   │   ├── Commands/
│   │   │   ├── ImportTransactions/
│   │   │   ├── CreateTransaction/
│   │   │   ├── CreateBudget/
│   │   │   ├── ApplyRules/
│   │   │   └── SyncData/
│   │   ├── Queries/
│   │   │   ├── GetNetWorth/
│   │   │   ├── GetTransactions/
│   │   │   ├── GetBudgetSummary/
│   │   │   ├── GetCashFlowForecast/
│   │   │   └── GetPortfolioPerformance/
│   │   ├── Services/
│   │   │   ├── RulesEngineService.cs
│   │   │   ├── ImportOrchestrator.cs
│   │   │   ├── ForecastingService.cs
│   │   │   └── SyncService.cs
│   │   ├── Interfaces/
│   │   │   ├── IAccountRepository.cs
│   │   │   ├── ITransactionRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── DTOs/
│   │   ├── Mappings/
│   │   └── Prospero.Application.csproj
│   │
│   ├── Prospero.Infrastructure/             # Infrastructure (data access, external)
│   │   ├── Data/
│   │   │   ├── ProsperoDbContext.cs
│   │   │   ├── Configurations/              # EF Core entity configurations
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   ├── Identity/
│   │   │   ├── IdentityService.cs
│   │   │   └── TokenService.cs
│   │   ├── Encryption/
│   │   │   └── FieldEncryptionService.cs
│   │   ├── Importers/
│   │   │   ├── CsvTransactionImporter.cs
│   │   │   ├── OfxTransactionImporter.cs
│   │   │   └── QifTransactionImporter.cs
│   │   ├── PriceFeeds/
│   │   │   └── YahooFinancePriceFeed.cs
│   │   ├── Plugins/
│   │   │   ├── PluginLoader.cs
│   │   │   └── PluginManager.cs
│   │   ├── AI/
│   │   │   └── OllamaCategorizer.cs
│   │   └── Prospero.Infrastructure.csproj
│   │
│   ├── Prospero.Api/                        # ASP.NET Core API host
│   │   ├── Endpoints/
│   │   │   ├── AccountEndpoints.cs
│   │   │   ├── TransactionEndpoints.cs
│   │   │   ├── ImportEndpoints.cs
│   │   │   ├── BudgetEndpoints.cs
│   │   │   ├── RuleEndpoints.cs
│   │   │   ├── ReportEndpoints.cs
│   │   │   ├── AuthEndpoints.cs
│   │   │   └── SyncEndpoints.cs
│   │   ├── Middleware/
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Prospero.Api.csproj
│   │
│   └── Prospero.Web/                        # Blazor WASM PWA
│       ├── wwwroot/
│       │   ├── index.html
│       │   ├── manifest.json
│       │   ├── service-worker.js
│       │   └── css/
│       ├── Layout/
│       │   ├── MainLayout.razor
│       │   └── NavMenu.razor
│       ├── Pages/
│       │   ├── Dashboard.razor
│       │   ├── Accounts/
│       │   ├── Transactions/
│       │   ├── Import/
│       │   ├── Budget/
│       │   ├── Reports/
│       │   ├── Settings/
│       │   └── Auth/
│       ├── Components/
│       │   ├── Charts/
│       │   ├── Forms/
│       │   ├── Shared/
│       │   └── OfflineIndicator.razor
│       ├── Services/
│       │   ├── LocalStorageService.cs        # IndexedDB wrapper
│       │   ├── SyncService.cs                # Offline sync orchestration
│       │   ├── ApiClient.cs                  # Typed HTTP client
│       │   └── AuthStateProvider.cs
│       ├── Program.cs
│       └── Prospero.Web.csproj
│
├── tests/
│   ├── Prospero.Domain.Tests/
│   ├── Prospero.Application.Tests/
│   ├── Prospero.Infrastructure.Tests/
│   ├── Prospero.Api.Tests/                   # Integration tests
│   └── Prospero.Web.Tests/                   # bUnit component tests
│
├── plugins/                                  # Example/community plugins
│   └── Prospero.Plugin.Template/
│
├── docker/
│   ├── docker-compose.yml                    # Production compose
│   ├── docker-compose.dev.yml                # Development overrides
│   ├── Dockerfile.api                        # Multi-stage API build
│   └── caddy/
│       └── Caddyfile                         # Auto-HTTPS reverse proxy config
│
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                            # Build, test, lint
│   │   ├── publish.yml                       # Build & push container images
│   │   └── release.yml                       # Semantic versioning & release
│   ├── instructions/                         # Copilot instruction files
│   └── copilot-instructions.md
│
├── Prospero.sln
├── Directory.Build.props                     # Shared build properties
├── Directory.Packages.props                  # Central package management
├── .editorconfig
├── .gitignore
└── README.md
```

---

## 4. Domain Model

### Core Entities

```
┌─────────────────────┐        ┌─────────────────────┐
│       User          │        │     Household        │
│─────────────────────│        │─────────────────────│
│ Id                  │───┐    │ Id                  │
│ Email               │   │    │ Name                │
│ DisplayName         │   └───▶│ Members: User[]     │
│ Preferences         │        └─────────────────────┘
└──────────┬──────────┘
           │ owns
           ▼
┌─────────────────────┐        ┌─────────────────────┐
│      Account        │        │    AccountType       │
│─────────────────────│        │─────────────────────│
│ Id                  │───────▶│ Investment           │
│ Name                │        │ Banking              │
│ AccountType         │        │ Credit               │
│ AccountSubType      │        │ Property             │
│ Currency            │        │ Loan                 │
│ Institution         │        └─────────────────────┘
│ IsShared            │
│ CurrentBalance      │        ┌─────────────────────┐
│ Owner: User         │        │  AccountSubType      │
└──────────┬──────────┘        │─────────────────────│
           │                   │ RRSP, TFSA,          │
           │ has               │ NonRegistered, RESP,  │
           ▼                   │ Chequing, Savings,    │
┌─────────────────────┐        │ CreditCard, Mortgage, │
│    Transaction      │        │ LineOfCredit, Property │
│─────────────────────│        └─────────────────────┘
│ Id                  │
│ Date                │
│ Amount: Money       │
│ Description         │
│ NormalizedPayee     │
│ Category            │
│ Tags[]              │
│ Type (Debit/Credit) │
│ IsReconciled        │
│ ImportBatchId       │
│ Notes               │
└─────────────────────┘

┌─────────────────────┐        ┌─────────────────────┐
│     Holding         │        │       Lot            │
│─────────────────────│        │─────────────────────│
│ Id                  │        │ Id                  │
│ Account             │───────▶│ Holding             │
│ Symbol              │        │ PurchaseDate        │
│ Name                │        │ Quantity            │
│ Quantity            │        │ CostPerUnit: Money  │
│ CurrentPrice: Money │        │ TotalCost: Money    │
│ MarketValue: Money  │        └─────────────────────┘
│ BookValue: Money    │
│ Lots: Lot[]         │
└─────────────────────┘

┌─────────────────────┐        ┌─────────────────────┐
│      Budget         │        │  RecurringTxn       │
│─────────────────────│        │─────────────────────│
│ Id                  │        │ Id                  │
│ Category            │        │ Account             │
│ Amount: Money       │        │ Amount: Money       │
│ Month               │        │ Description         │
│ Year                │        │ Category            │
│ RolloverEnabled     │        │ Frequency           │
│ User                │        │ StartDate           │
└─────────────────────┘        │ EndDate?            │
                               │ NextOccurrence      │
┌─────────────────────┐        └─────────────────────┘
│   ImportMapping     │
│─────────────────────│        ┌─────────────────────┐
│ Id                  │        │ CategorizationRule  │
│ Name                │        │─────────────────────│
│ FileFormat          │        │ Id                  │
│ ColumnMappings{}    │        │ Name                │
│ Institution         │        │ Priority (order)    │
│ User                │        │ Conditions (JSON)   │
└─────────────────────┘        │ Actions (JSON)      │
                               │ IsEnabled           │
                               │ User                │
                               └─────────────────────┘
```

### Value Objects

```csharp
// Money: always pairs amount with currency
public readonly record struct Money(decimal Amount, string CurrencyCode = "CAD");

// Smart enum for account types - extensible per locale
public record AccountType(string Code, string DisplayName, string Category);
```

---

## 5. Phased Implementation

### Phase 1: Foundation (Weeks 1–3)

**Goal:** Working .NET Aspire app with basic CRUD, auth, and data model.

| Task | Description | Estimate |
|------|-------------|----------|
| 1.1 | Create solution structure, all `.csproj` files, `Directory.Build.props` | 2h |
| 1.2 | Set up Aspire AppHost with PostgreSQL and API project | 2h |
| 1.3 | Implement domain entities: Account, Transaction, User, Money | 4h |
| 1.4 | Set up EF Core with PostgreSQL, entity configurations, initial migration | 4h |
| 1.5 | Implement local Identity auth (register, login, JWT tokens) | 4h |
| 1.6 | Implement OpenID Connect (Google + Microsoft) | 3h |
| 1.7 | Build Minimal API endpoints: Accounts CRUD, Transactions CRUD | 4h |
| 1.8 | Scaffold Blazor WASM project with MudBlazor/Fluent UI | 3h |
| 1.9 | Build basic pages: Login, Account List, Account Detail, Add Transaction | 6h |
| 1.10 | Set up Docker multi-stage build for API | 2h |
| 1.11 | Set up docker-compose.yml with PostgreSQL + API + Caddy (HTTPS) | 3h |
| 1.12 | Write domain unit tests and API integration tests | 4h |
| 1.13 | Set up GitHub Actions CI pipeline (build + test) | 2h |

**Deliverable:** Login, create accounts, manually add transactions, view account balances. Runs in dev mode via Aspire and in production via Docker Compose with HTTPS.

---

### Phase 2: Ingestion Pipeline (Weeks 4–6)

**Goal:** Import transactions from files with configurable mapping and rules.

| Task | Description | Estimate |
|------|-------------|----------|
| 2.1 | Implement CSV parser with CsvHelper and dynamic column mapping | 4h |
| 2.2 | Implement OFX/QFX parser | 4h |
| 2.3 | Implement QIF parser | 3h |
| 2.4 | Build Column Mapping UI: preview file, assign columns, save mapping | 6h |
| 2.5 | Implement duplicate detection (hash-based + fuzzy date/amount match) | 4h |
| 2.6 | Build import preview page (review before commit) | 4h |
| 2.7 | Integrate Microsoft.RulesEngine for transaction categorization | 4h |
| 2.8 | Build Rules UI: create/edit/order rules visually | 6h |
| 2.9 | Implement auto-apply rules on import | 2h |
| 2.10 | Transfer between accounts (linked transactions) | 3h |
| 2.11 | Import history with undo/rollback support | 3h |
| 2.12 | Tests for all importers and rules engine | 4h |

**Deliverable:** Import CSV/QFX/QIF files, map columns, auto-categorize via rules, review & commit.

---

### Phase 3: Budgeting & Recurring (Weeks 7–9)

**Goal:** Budget management, recurring transactions, and cash flow forecasting.

| Task | Description | Estimate |
|------|-------------|----------|
| 3.1 | Implement Budget entity and CRUD endpoints | 3h |
| 3.2 | Build Budget UI: set budgets per category, monthly view | 4h |
| 3.3 | Budget vs Actual calculations and display | 4h |
| 3.4 | Category management (create, group, reorder categories) | 3h |
| 3.5 | Implement RecurringTransaction entity and CRUD | 3h |
| 3.6 | Build Recurring Transactions UI: define patterns, preview schedule | 4h |
| 3.7 | Auto-generation of future expected transactions | 3h |
| 3.8 | Cash flow forecasting engine (project balances forward) | 6h |
| 3.9 | Forecast visualization (line chart with actual vs projected) | 4h |
| 3.10 | Minimum balance alerts | 2h |
| 3.11 | Tests for budgeting and forecasting logic | 4h |

**Deliverable:** Set budgets, define recurring transactions, see cash flow forecast.

---

### Phase 4: Dashboards & Offline-First (Weeks 10–13)

**Goal:** Rich dashboards, charts, and offline PWA capability.

| Task | Description | Estimate |
|------|-------------|----------|
| 4.1 | Net Worth dashboard: summary cards, time-series chart, asset allocation | 6h |
| 4.2 | Spending analysis: category breakdown, trends, payee ranking | 6h |
| 4.3 | Cash flow dashboard: income vs expenses, savings rate | 4h |
| 4.4 | Debt overview: balances, rates, amortization charts | 4h |
| 4.5 | Implement service worker for app shell caching | 4h |
| 4.6 | Implement IndexedDB local store (account summaries, recent txns) | 6h |
| 4.7 | Build sync engine: queue offline changes, sync on reconnect | 8h |
| 4.8 | Conflict resolution strategy (last-write-wins + user review) | 4h |
| 4.9 | Offline/online status indicator in UI | 2h |
| 4.10 | PWA manifest and install prompt | 2h |
| 4.11 | Tests for sync engine and offline scenarios | 4h |

**Deliverable:** Full dashboard suite. App works offline, syncs when back online.

---

### Phase 5: Investments & Advanced Features (Weeks 14–18)

**Goal:** Investment tracking, Ollama AI, plugin system, multi-user households.

| Task | Description | Estimate |
|------|-------------|----------|
| 5.1 | Holding and Lot entities, CRUD endpoints | 4h |
| 5.2 | Investment account UI: holdings list, lot details | 4h |
| 5.3 | Portfolio performance calculations (TWR, MWR) | 6h |
| 5.4 | Price feed plugin interface and Yahoo Finance implementation | 4h |
| 5.5 | Investment dashboard: portfolio value, gain/loss, allocation | 6h |
| 5.6 | Plugin loader: assembly scanning, registration, configuration | 6h |
| 5.7 | Ollama integration: transaction categorization service | 6h |
| 5.8 | AI categorization UI: suggestions, confidence, batch processing | 4h |
| 5.9 | Household entity and multi-user sharing | 4h |
| 5.10 | Shared vs private account visibility | 3h |
| 5.11 | Role-based access control (Admin/Member) | 3h |
| 5.12 | Property tracking: value, expenses, mortgage amortization | 4h |
| 5.13 | Data export (CSV, JSON) and backup/restore | 4h |
| 5.14 | Automated PostgreSQL backups in Docker Compose | 3h |
| 5.15 | Tests for investments, plugins, and AI integration | 6h |

**Deliverable:** Full investment tracking, AI categorization, plugin system, multi-user households.

---

### Phase 6: Polish & Hardening (Weeks 19–21)

**Goal:** Production readiness, security hardening, CI/CD publishing.

| Task | Description | Estimate |
|------|-------------|----------|
| 6.1 | Field-level encryption for sensitive data (account numbers, etc.) | 4h |
| 6.2 | Audit logging for all data mutations | 3h |
| 6.3 | Rate limiting on API endpoints | 2h |
| 6.4 | Security headers (CSP, HSTS, X-Content-Type-Options) | 2h |
| 6.5 | Two-factor authentication (TOTP) | 4h |
| 6.6 | Session management and concurrent session handling | 2h |
| 6.7 | GitHub Actions: build & push container images to ghcr.io | 3h |
| 6.8 | GitHub Actions: release workflow with semantic versioning | 3h |
| 6.9 | Health checks for all services (DB, Ollama, etc.) | 2h |
| 6.10 | Documentation: README, deployment guide, development setup | 4h |
| 6.11 | End-to-end testing with Playwright | 6h |
| 6.12 | Performance testing and optimization | 4h |

**Deliverable:** Production-hardened, secure, documented, and CI/CD automated.

---

## 6. Infrastructure & DevOps

### Aspire AppHost (Development)

```csharp
// Prospero.AppHost/Program.cs (conceptual)
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("prospero");

var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithEndpoint(11434, 11434, name: "ollama-api");

var api = builder.AddProject<Projects.Prospero_Api>("api")
    .WithReference(postgres)
    .WithReference(ollama);

builder.AddProject<Projects.Prospero_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

### Docker Compose (Production)

```yaml
# docker/docker-compose.yml (conceptual)
services:
  caddy:
    image: caddy:2-alpine
    ports: ["443:443", "80:80"]
    volumes:
      - ./caddy/Caddyfile:/etc/caddy/Caddyfile
      - caddy_data:/data

  api:
    image: ghcr.io/davidhayesbc/prospero-api:latest
    environment:
      - ConnectionStrings__prospero=Host=postgres;Database=prospero;...
      - Auth__Google__ClientId=${GOOGLE_CLIENT_ID}
    depends_on: [postgres]

  postgres:
    image: postgres:17-alpine
    environment:
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data

  ollama:  # Optional
    image: ollama/ollama
    profiles: ["ai"]
    volumes:
      - ollama_data:/root/.ollama

  backup:  # Scheduled backups
    image: prodrigestivill/postgres-backup-local
    profiles: ["backup"]
    depends_on: [postgres]
    volumes:
      - ./backups:/backups

volumes:
  pgdata:
  caddy_data:
  ollama_data:
```

### CI/CD Pipeline

```
Push to main ──▶ Build ──▶ Test ──▶ Lint ──▶ Publish Image ──▶ Release
                  │          │        │         │
                  │          │        │         └─▶ ghcr.io/davidhayesbc/prospero-api:sha
                  │          │        └─▶ dotnet format --verify-no-changes
                  │          └─▶ dotnet test (unit + integration)
                  └─▶ dotnet build --configuration Release
```

---

## Summary Timeline

| Phase | Duration | Key Milestone |
|-------|----------|--------------|
| **Phase 1: Foundation** | Weeks 1–3 | Auth, accounts, transactions, Docker |
| **Phase 2: Ingestion** | Weeks 4–6 | CSV/QFX/QIF import, rules engine |
| **Phase 3: Budgeting** | Weeks 7–9 | Budgets, recurring, forecasting |
| **Phase 4: Dashboards & PWA** | Weeks 10–13 | Charts, offline-first, sync |
| **Phase 5: Investments & Advanced** | Weeks 14–18 | Portfolio, AI, plugins, multi-user |
| **Phase 6: Polish** | Weeks 19–21 | Security, CI/CD, documentation |

> **Total estimated effort:** ~21 weeks of part-time development (assuming ~15-20 hours/week)
