# Prospero — Personal Finance Tracker — Copilot Instructions

## Project Overview

Prospero is an offline-first, self-hosted personal finance tracker built on .NET 10 with .NET Aspire. It tracks investment accounts (RRSP, TFSA, non-registered), banking, credit cards, mortgages, property, and other financial assets. It features a configurable ingestion pipeline, rules-based categorization, budgeting, forecasting, and portfolio tracking.

### Core Principles

-   **Offline-first**: Blazor WASM PWA with IndexedDB local store; sync when server is reachable
-   **Null avoidance**: Minimize null usage; prefer Option/Result patterns, empty collections, and default values
-   **Functional style**: Favor immutability, pure functions, LINQ, and declarative code
-   **Security-first**: Encryption at rest and in transit; OWASP compliance; no hardcoded secrets
-   **Extensible**: Plugin architecture for importers, price feeds, and custom rules

## Architecture

```
src/
  Prospero.AppHost/           - .NET Aspire orchestrator (dev)
  Prospero.ServiceDefaults/   - Shared Aspire service defaults
  Prospero.Domain/            - Entities, value objects, interfaces (pure C#, no dependencies)
  Prospero.Application/       - Use cases, CQRS commands/queries, services
  Prospero.Infrastructure/    - EF Core, importers, plugins, AI, encryption
  Prospero.Api/               - ASP.NET Core Minimal API host
  Prospero.Web/               - Blazor WebAssembly PWA
tests/
  Prospero.Domain.Tests/
  Prospero.Application.Tests/
  Prospero.Infrastructure.Tests/
  Prospero.Api.Tests/
  Prospero.Web.Tests/
docker/
  docker-compose.yml          - Production deployment
  Dockerfile.api              - Multi-stage API build
```

## Layer Responsibilities

-   **Domain**: Pure C# entities, value objects, domain events, interfaces. NO external dependencies.
-   **Application**: Use cases orchestrated as Commands/Queries. References Domain only.
-   **Infrastructure**: EF Core, file parsers, plugins, Ollama client. Implements Domain/Application interfaces.
-   **Api**: Thin Minimal API layer. Maps HTTP to Application commands/queries. Handles auth, validation, error responses.
-   **Web**: Blazor WASM PWA. Calls API via typed HttpClient. Manages offline state in IndexedDB.

## Key Domain Types

-   `Account` - Financial account (investment, banking, credit, property)
-   `Transaction` - Individual financial transaction with category and tags
-   `TransactionSplit` - Logical child split line of a Transaction (category + amount); splits must sum to parent
-   `Money` - Value object: `decimal Amount` + `string CurrencyCode`
-   `Holding` - Investment position (symbol, quantity, price)
-   `Lot` - Purchase lot for cost basis tracking
-   `Budget` - Monthly budget per category (split-aware: uses split line categories)
-   `SinkingFund` - Savings target for lump-sum expenses (target amount, due date, monthly set-aside)
-   `RecurringTransaction` - Expected recurring income/expense pattern
-   `ForecastScenario` - Named net worth forecast with per-account/asset-class growth rate assumptions
-   `CategorizationRule` - User-defined rule (conditions + actions as JSON); can include auto-split templates
-   `ImportMapping` - Saved column mapping for a file format/institution

## Coding Standards

### Nullable Reference Types

All projects have `<Nullable>enable</Nullable>`. This provides compile-time null safety:

```csharp
// Parameters are non-nullable by default
public void Process(Account account) { }

// Use ? suffix for explicitly nullable types
public Transaction? FindByExternalId(string externalId) { }

// Combine with runtime checks for defense in depth on public APIs
public void Process(Account account)
{
    ArgumentNullException.ThrowIfNull(account);
}
```

### Null Avoidance (Critical)

```csharp
// ❌ AVOID: Nullable returns and null checks scattered through code
public Account? FindAccount(string name) { ... }
if (account == null) return;

// ✅ PREFER: Option pattern or Result types
public Option<Account> FindAccount(string name) { ... }
public Result<Account, Error> FindAccount(string name) { ... }

// ✅ PREFER: Empty collections over null
public IReadOnlyCollection<string> Tags { get; } = Array.Empty<string>();

// ✅ PREFER: Guard clauses that throw early for true invariant violations
public void Debit(Money amount)
{
    ArgumentNullException.ThrowIfNull(amount);
    if (amount.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
}
```

### Functional Style

```csharp
// ❌ AVOID: Mutable state, imperative loops
var totals = new List<Money>();
foreach (var txn in transactions) { totals.Add(txn.Amount); }

// ✅ PREFER: LINQ transformations
var total = transactions.Select(t => t.Amount.Amount).Sum();

// ✅ PREFER: Records for immutable data
public readonly record struct Money(decimal Amount, string CurrencyCode = "CAD");

// ✅ PREFER: Expression-bodied members
public Money Negate() => this with { Amount = -Amount };

// ✅ PREFER: Pure functions
public decimal CalculateNetWorth(IEnumerable<Account> accounts) =>
    accounts.Sum(a => a.IsLiability ? -a.CurrentBalance.Amount : a.CurrentBalance.Amount);
```

### Performance Considerations

```csharp
// ✅ PREFER: IReadOnlyList<T> over IEnumerable<T> when size is known
// ✅ PREFER: Single enumeration, materialize collections when reused
// ✅ PREFER: ValueTask for hot-path async methods
// ✅ PREFER: ReadOnlySpan<T>/Memory<T> for parsing operations
// ✅ PREFER: Batch database operations over N+1 queries
```

### Financial Precision

```csharp
// ✅ ALWAYS use decimal for monetary amounts, never float/double
// ✅ ALWAYS pair amounts with currency codes (Money value object)
// ✅ ALWAYS use banker's rounding (MidpointRounding.ToEven) for financial calculations
// ✅ ALWAYS store amounts in minor units when precision matters (cents)
```

### API Design (Minimal APIs)

```csharp
// ✅ PREFER: Resource-oriented endpoints
app.MapGet("/api/accounts/{id}", handler);
app.MapPost("/api/accounts", handler);

// ✅ PREFER: Appropriate HTTP status codes
return Results.Created($"/api/accounts/{account.Id}", account);
return Results.NoContent();
return Results.Problem("Account not found", statusCode: 404);

// ✅ PREFER: FluentValidation for request validation
// ✅ PREFER: Typed Results for compile-time response type safety
```

### Blazor Components

```csharp
// ✅ PREFER: Component parameters with [Parameter] and [EditorRequired]
[Parameter, EditorRequired] public Account Account { get; set; } = default!;

// ✅ PREFER: EventCallback for parent communication
[Parameter] public EventCallback<Transaction> OnTransactionAdded { get; set; }

// ✅ PREFER: Inject services via @inject or [Inject]
// ✅ PREFER: Override ShouldRender() for performance-critical components
// ✅ PREFER: @key for list rendering optimization
```

## Testing Conventions

-   Use xUnit with `[Fact]` and `[Theory]` attributes
-   Test file naming: `{ClassName}Tests.cs`
-   Follow Arrange-Act-Assert pattern
-   Use descriptive test names: `MethodName_Scenario_ExpectedResult`
-   Use Testcontainers for integration tests needing PostgreSQL
-   Use bUnit for Blazor component tests
-   Use Bogus for realistic test data generation

## Formatting

-   Use CSharpier for formatting
-   Do not manually format code — let the tool handle it

## Diagrams

-   **Always use Mermaid** for diagrams in Markdown files (architecture, data flow, entity relationships, sequence diagrams, CI/CD pipelines, etc.)
-   Do not use ASCII art or box-drawing characters for diagrams
-   Use appropriate Mermaid diagram types: `graph`/`flowchart` for architecture and flows, `classDiagram` for domain models, `sequenceDiagram` for interaction flows, `erDiagram` for database schemas
-   Wrap Mermaid diagrams in fenced code blocks with the `mermaid` language identifier

## File Organization

-   One primary type per file
-   File name matches primary type name
-   Keep interfaces in the Domain project
-   Keep implementations in Infrastructure project
-   Keep use cases (commands/queries) in Application project
-   Keep API endpoint groups in separate files under `Endpoints/`

## Naming Conventions

-   Interfaces: `I{Name}` (e.g., `IAccountRepository`, `ITransactionImporter`)
-   Entities: Descriptive singular nouns (e.g., `Account`, `Transaction`)
-   Value Objects: Descriptive names (e.g., `Money`, `DateRange`)
-   Commands: `{Verb}{Noun}Command` (e.g., `CreateAccountCommand`, `ImportTransactionsCommand`)
-   Queries: `Get{Noun}Query` (e.g., `GetNetWorthQuery`, `GetTransactionsQuery`)
-   Handlers: `{Command/Query}Handler` (e.g., `CreateAccountCommandHandler`)
-   Endpoints: `{Resource}Endpoints` (e.g., `AccountEndpoints`, `TransactionEndpoints`)
-   Plugin interfaces: `I{Noun}{Verb}er` (e.g., `ITransactionImporter`, `IPriceFeedProvider`)
