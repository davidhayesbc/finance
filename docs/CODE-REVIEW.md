# Code Review: Privestio — Detailed Recommendations

**Date:** 2026-03-31
**Scope:** Full codebase review (~720 C# files, 54 Razor pages, 30+ domain entities)
**Framework:** .NET 10, Blazor WASM, Minimal API, EF Core, PostgreSQL, .NET Aspire

---

## P0 — Critical (Must Fix)

---

### 1. Broken Optimistic Concurrency

**Files:**
- `src/Privestio.Infrastructure/Data/PrivestioDbContext.cs`
- `src/Privestio.Domain/Entities/BaseEntity.cs`

**Problem:**
`BaseEntity.Version` is configured as `.IsConcurrencyToken()` in `OnModelCreating`, but nothing ever increments it. EF Core's `IsConcurrencyToken` only adds `WHERE Version = @old` to UPDATE statements — it does not auto-increment the value. After the first save, `Version` remains `0` forever, making concurrent edit detection non-functional.

**Impact:** Two users editing the same account/transaction simultaneously will silently overwrite each other's changes. This is a data-integrity issue in a financial application.

**Recommendation:**
Option A (preferred) — Switch to PostgreSQL's built-in `xmin` system column:
```csharp
// In PrivestioDbContext.OnModelCreating, replace the Version loop with:
foreach (var entityType in modelBuilder.Model.GetEntityTypes()
    .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
{
    modelBuilder.Entity(entityType.ClrType).UseXminAsConcurrencyToken();
}
```
Then remove the `Version` property from `BaseEntity` entirely (and its migration column) since `xmin` is managed automatically by PostgreSQL.

Option B — Increment `Version` in `SaveChangesAsync`:
```csharp
// In PrivestioDbContext.SaveChangesAsync, inside the UpdateTimestamps loop:
if (entry.State == EntityState.Modified && entry.Entity is BaseEntity baseEntity)
{
    baseEntity.Version++;
}
```
This requires `Version` to keep its public setter (for EF), but Option A is cleaner.

**Testing:** Add a concurrency test that reads an entity in two DbContext instances, modifies both, saves one, then asserts the second save throws `DbUpdateConcurrencyException`.

---

### 2. JWT Stored in localStorage — Vulnerable to XSS

**File:** `src/Privestio.Web/Services/AuthService.cs` (line ~136)

**Problem:**
The JWT access token is stored in `localStorage` via JS interop. `localStorage` is accessible to any JavaScript running on the page. A single XSS vulnerability (even from a third-party library) would allow an attacker to steal the token and impersonate the user.

**Impact:** Complete account takeover if any XSS vector exists.

**Recommendation:**
Implement a Backend-for-Frontend (BFF) pattern or switch to `httpOnly` cookie-based auth:

1. Add a `/api/v1/auth/login` response that sets an `httpOnly`, `Secure`, `SameSite=Strict` cookie instead of returning the token in the response body.
2. Remove all `localStorage` token operations from `AuthService.cs`.
3. Add cookie authentication scheme alongside JWT in `Program.cs`:
```csharp
builder.Services.AddAuthentication()
    .AddCookie("Cookies", options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });
```
4. Update the CORS/anti-forgery configuration accordingly.

**Alternative (simpler):** If BFF is too large a change, at minimum move token storage to `sessionStorage` (cleared on tab close) and add a strict Content-Security-Policy to mitigate XSS. This is not a full fix but reduces the window of exposure.

---

### 3. No Refresh Token / No Token Revocation

**File:** `src/Privestio.Api/Endpoints/AuthEndpoints.cs`

**Problem:**
The JWT has a 1-hour lifetime with no refresh mechanism. There is no way to revoke a compromised token. `ExpiresIn` is hardcoded to `3600` rather than derived from the actual token expiry.

**Impact:** A stolen token is valid for the full hour with no mitigation available.

**Recommendation:**
1. Reduce access token lifetime to 15 minutes.
2. Add a `RefreshToken` entity in the domain:
```csharp
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } // cryptographically random
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByToken { get; private set; }
}
```
3. Add `POST /api/v1/auth/refresh` endpoint that validates the refresh token, issues a new access + refresh token pair, and rotates the old refresh token.
4. Add `POST /api/v1/auth/revoke` endpoint for logout.
5. Compute `ExpiresIn` from the actual token expiry:
```csharp
ExpiresIn = (int)(tokenExpiry - DateTime.UtcNow).TotalSeconds
```

---

### 4. No Rate Limiting on Auth Endpoints

**Files:**
- `src/Privestio.Api/Endpoints/AuthEndpoints.cs` (lines 25, 31)
- `src/Privestio.Api/Program.cs`

**Problem:**
`/register` and `/login` are `AllowAnonymous` with no rate limiting. Account lockout mitigates per-account brute force, but credential-stuffing across many accounts and registration spam are unmitigated.

**Recommendation:**
Add `Microsoft.AspNetCore.RateLimiting` middleware in `Program.cs`:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// In the pipeline:
app.UseRateLimiter();
```
Apply to auth endpoints:
```csharp
group.MapPost("/register", ...).RequireRateLimiting("auth");
group.MapPost("/login", ...).RequireRateLimiting("auth");
```
Consider a stricter per-IP policy for `/login` (e.g., 5 attempts/minute) and a separate policy for `/register` (e.g., 3 attempts/minute).

---

### 5. API Integration Tests Project is Empty

**File:** `tests/Privestio.Api.Tests/`

**Problem:**
The project references `Microsoft.AspNetCore.Mvc.Testing` and `Testcontainers.PostgreSql` but contains zero test files. 30 endpoint groups have no integration test coverage.

**Recommendation:**
Create integration tests using `WebApplicationFactory<Program>` with Testcontainers for a real PostgreSQL instance. Priority test targets:

1. **Auth flow:** Register → Login → access protected endpoint → token expiry behavior
2. **Transaction CRUD:** Create → Read → Update splits → Delete → verify soft-delete
3. **Import flow:** Upload CSV → preview → commit → verify transactions created → rollback
4. **Account operations:** Create → update → deactivate → verify child entity behavior
5. **Authorization:** Verify user A cannot access user B's resources (IDOR protection)

Example structure:
```csharp
public class TransactionEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_WithValidData_Returns201()
    {
        // Arrange: register, login, create account
        // Act: POST /api/v1/transactions
        // Assert: 201, response matches request, GET returns same data
    }
}
```

---

### 6. Infrastructure Tests Not Run in CI

**File:** `.github/workflows/ci.yml`

**Problem:**
The CI pipeline only runs Domain and Application tests:
```yaml
dotnet test tests/Privestio.Domain.Tests
dotnet test tests/Privestio.Application.Tests
```
`Privestio.Infrastructure.Tests` (9 test files covering importers, price feeds, rule evaluation) is never executed in CI.

**Recommendation:**
Add the missing test projects to the CI pipeline:
```yaml
- name: Run Infrastructure Tests
  run: dotnet test tests/Privestio.Infrastructure.Tests --no-build --verbosity normal

- name: Run API Tests
  run: dotnet test tests/Privestio.Api.Tests --no-build --verbosity normal
```
For E2E tests, add a separate workflow or a manual trigger job:
```yaml
e2e-tests:
  if: github.event_name == 'workflow_dispatch'
  runs-on: ubuntu-latest
  steps:
    - name: Run E2E Tests
      run: dotnet test tests/Privestio.E2E.Tests --filter "Category=E2E"
```

---

### 7. Dockerfile HEALTHCHECK Uses Missing Tool

**File:** `docker/Dockerfile.api` (line ~47)

**Problem:**
The HEALTHCHECK uses `curl`, which is not installed in `mcr.microsoft.com/dotnet/aspnet:10.0`. The health check always fails, so container orchestrators cannot monitor API health.

**Recommendation:**
Replace with a .NET-based health check or remove the Dockerfile-level check in favor of the compose-level one (which uses `wget` or a different mechanism):

Option A — Remove from Dockerfile, keep compose-level only:
```dockerfile
# Remove the HEALTHCHECK instruction from Dockerfile.api
# The docker-compose.yml healthcheck handles this
```

Option B — Use a .NET health endpoint probe:
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD ["dotnet", "/app/healthcheck.dll"] || exit 1
```
This requires a tiny separate console app, so Option A is simpler.

Option C — Install wget in the Dockerfile:
```dockerfile
RUN apt-get update && apt-get install -y --no-install-recommends wget && rm -rf /var/lib/apt/lists/*
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/healthz || exit 1
```

---

## P1 — High (Should Fix Soon)

---

### 8. Domain Entity Encapsulation Breaches

**Files:**
- `src/Privestio.Domain/Entities/Transaction.cs`
- `src/Privestio.Domain/Entities/Account.cs`
- `src/Privestio.Domain/Entities/BaseEntity.cs`

**Problem:**
Core financial properties have public setters that allow external mutation without invariant enforcement:
- `Transaction.Amount`, `Date`, `Description`, `Type` — public setters mean the split-sum invariant can be silently violated
- `Account.CurrentBalance` — documented as "never set directly" but has a public setter
- `BaseEntity.IsDeleted`, `DeletedAt`, `Version` — public setters bypass `SoftDelete()` method

**Recommendation:**
Change all to `private set` and add domain methods:

For `Transaction.cs`:
```csharp
public decimal Amount { get; private set; }
public DateTime Date { get; private set; }
public string Description { get; private set; }
public TransactionType Type { get; private set; }

public void UpdateDetails(DateTime date, decimal amount, string description, TransactionType type)
{
    Date = date;
    Description = description ?? throw new ArgumentNullException(nameof(description));
    Type = type;

    if (Amount != amount)
    {
        Amount = amount;
        if (IsSplit && !ValidateSplitInvariant())
            throw new InvalidOperationException("Amount change violates split-sum invariant.");
    }
    UpdatedAt = DateTime.UtcNow;
}
```

For `Account.cs`:
```csharp
public decimal CurrentBalance { get; private set; }

public void UpdateBalance(decimal newBalance)
{
    CurrentBalance = newBalance;
    UpdatedAt = DateTime.UtcNow;
}
```

For `BaseEntity.cs`:
```csharp
public bool IsDeleted { get; private set; }
public DateTime? DeletedAt { get; private set; }
public long Version { get; private set; } // or remove if using xmin
```

**Note:** EF Core can map to private setters via backing fields. Update entity configurations if needed:
```csharp
builder.Property(e => e.Amount).HasField("_amount"); // if using backing fields
```

---

### 9. Money Value Object Default Constructor Produces Invalid State

**File:** `src/Privestio.Domain/ValueObjects/Money.cs`

**Problem:**
As a `record struct`, `default(Money)` and `new Money()` produce `Amount = 0m, CurrencyCode = null`. This bypasses the parameterized constructor's default of `"CAD"`. Using the default in arithmetic operators causes `NullReferenceException` instead of a meaningful domain error.

**Recommendation:**
Add defensive checks in operators and a factory method:
```csharp
public readonly record struct Money(decimal Amount, string CurrencyCode = "CAD")
{
    // Add a guard for the default struct footgun
    private void EnsureValid()
    {
        if (string.IsNullOrEmpty(CurrencyCode))
            throw new InvalidOperationException(
                "Money must have a currency code. Use Money.Zero(currency) instead of default(Money).");
    }

    public static Money Zero(string currencyCode) => new(0m, currencyCode);

    public static Money operator +(Money left, Money right)
    {
        left.EnsureValid();
        right.EnsureValid();
        if (left.CurrencyCode != right.CurrencyCode)
            throw new InvalidOperationException($"Cannot add {left.CurrencyCode} and {right.CurrencyCode}");
        return new Money(left.Amount + right.Amount, left.CurrencyCode);
    }

    // Apply same pattern to -, >, <, >=, <= operators
}
```

Also add currency code validation:
```csharp
public Money
{
    if (CurrencyCode is not null && (CurrencyCode.Length != 3 || CurrencyCode != CurrencyCode.ToUpperInvariant()))
        throw new ArgumentException($"Invalid currency code: {CurrencyCode}. Must be a 3-letter uppercase ISO 4217 code.");
}
```

---

### 10. Silent Exception Swallowing in All Frontend Services

**Files:** All 28 services in `src/Privestio.Web/Services/`

**Problem:**
Every HTTP service uses bare `catch { return null; }` or `catch { return []; }`. The UI cannot distinguish network failures, auth expiry, validation errors, or server errors. Example from `AccountService.cs`:
```csharp
catch { return null; } // Swallows everything
```

**Recommendation:**
Create a base service class with typed error handling:

```csharp
// src/Privestio.Web/Services/ApiServiceBase.cs
public abstract class ApiServiceBase
{
    protected readonly HttpClient Http;
    protected readonly AuthService Auth;
    private readonly ILogger _logger;

    protected async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        try
        {
            var response = await Http.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Auth.Logout();
                return ApiResult<T>.Unauthorized();
            }

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<T>();
            return ApiResult<T>.Success(data!);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "API request failed: {Url}", url);
            return ApiResult<T>.NetworkError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Timeout();
        }
    }
}

public record ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public ApiErrorKind ErrorKind { get; init; }

    public static ApiResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static ApiResult<T> Unauthorized() => new() { ErrorKind = ApiErrorKind.Unauthorized };
    public static ApiResult<T> NetworkError(string msg) => new() { ErrorKind = ApiErrorKind.Network, ErrorMessage = msg };
    public static ApiResult<T> Timeout() => new() { ErrorKind = ApiErrorKind.Timeout };
}

public enum ApiErrorKind { None, Unauthorized, Network, Timeout, Validation, ServerError }
```

Migrate services incrementally — start with the most-used ones (`AccountService`, `TransactionService`, `AuthService`).

---

### 11. Inconsistent API Response Shapes

**Files:**
- `src/Privestio.Api/Endpoints/TransactionEndpoints.cs`
- `src/Privestio.Api/Endpoints/SecurityEndpoints.cs`
- Various other endpoint files

**Problem:**
- `TransactionEndpoints.GetTransactionByIdAsync` returns an anonymous object with hand-picked properties
- `TransactionEndpoints.CreateTransactionAsync` returns a *different* anonymous object shape for the same entity
- `SecurityEndpoints` uses `Results.BadRequest(new { error = ex.Message })` — ad-hoc error shape
- Other endpoints use `Results.ValidationProblem(...)` — RFC 7807 ProblemDetails
- `BulkCategorizeAsync` returns `new { UpdatedCount = updated }` — yet another shape

**Recommendation:**
1. **All entity responses** must use typed DTOs from `Privestio.Contracts`. Replace all anonymous objects:
```csharp
// Before (anonymous object):
return Results.Ok(new { transaction.Id, transaction.Date, ... });

// After (typed response):
return Results.Ok(TransactionMapper.ToResponse(transaction));
```

2. **All error responses** must use ProblemDetails (RFC 7807):
```csharp
// Before:
return Results.BadRequest(new { error = ex.Message });

// After:
return Results.Problem(
    detail: "The security symbol conflicts with an existing entry.",
    statusCode: StatusCodes.Status400BadRequest,
    title: "Validation Error");
```

3. **Audit all endpoint files** — replace every `new { ... }` return with either a Contracts DTO or `Results.Problem()`.

---

### 12. N+1 Query Patterns in Critical Services

**Files:**
- `src/Privestio.Application/Services/HistoricalValueTimelineService.cs` (lines 385-396, 513-524)
- `src/Privestio.Application/Commands/ImportTransactions/ImportTransactionsCommandHandler.cs` (lines 378-400)
- `src/Privestio.Application/Services/InvestmentPortfolioValuationService.cs`

**Problem:**
Price histories and FX rates are fetched one-at-a-time inside `foreach` loops. For a portfolio with 20 securities over 365 days, this generates hundreds of sequential database queries.

Additionally in `HistoricalValueTimelineService`:
- `history.FirstOrDefault(p => p.Date == date)` is a linear scan per date per account (line 117)
- `ConvertValue` (lines 693-697) does a full LINQ scan of a `SortedDictionary` ignoring its ordered nature
- Account histories are fetched sequentially when they're independent

**Recommendation:**

**Batch price fetches:**
```csharp
// Before:
foreach (var security in securities)
{
    var prices = await _priceHistoryRepo.GetBySecurityIdAsync(security.Id, from, to);
    ...
}

// After:
var securityIds = securities.Select(s => s.Id).ToList();
var allPrices = await _priceHistoryRepo.GetBySecurityIdsAsync(securityIds, from, to);
var pricesBySecurityId = allPrices.GroupBy(p => p.SecurityId).ToDictionary(g => g.Key, g => g.ToList());
```

Add `GetBySecurityIdsAsync` to `IPriceHistoryRepository`:
```csharp
Task<IReadOnlyList<PriceHistory>> GetBySecurityIdsAsync(
    IReadOnlyList<Guid> securityIds, DateTime from, DateTime to, CancellationToken ct = default);
```

**Use dictionary lookups instead of linear scans:**
```csharp
// Before:
history.FirstOrDefault(p => p.Date == date)

// After:
var historyByDate = history.ToDictionary(p => p.Date);
historyByDate.TryGetValue(date, out var point);
```

**Parallelize independent account calculations:**
```csharp
// Before:
foreach (var account in accounts)
    histories.Add(await GetAccountHistoryAsync(account, ...));

// After:
var tasks = accounts.Select(a => GetAccountHistoryAsync(a, ...));
var results = await Task.WhenAll(tasks);
```

**Fix FX rate lookup in SortedDictionary:**
```csharp
// Before: full LINQ scan
rates.Where(r => r.Key <= date).OrderByDescending(r => r.Key).FirstOrDefault()

// After: use SortedDictionary efficiently
// Convert to SortedList or use a binary search approach
```

---

### 13. CQRS Violation — Read Operations With Side Effects

**File:** `src/Privestio.Application/Services/InvestmentPortfolioValuationService.cs`

**Problem:**
`CalculateAsync` is a valuation query, but it writes to the database: persisting missing prices (line 327) and exchange rates (line 515). This violates CQRS command/query separation. Two concurrent calls for the same account can race on persisting the same price row.

**Recommendation:**
Split into two operations:

1. **Query path (read-only):** `CalculateAsync` uses only cached/existing data. If prices are stale, returns a `ValuationResult` with a `HasStaleData = true` flag.
2. **Command path (write):** `RefreshPricesCommand` fetches and persists missing prices. Can be triggered on-demand from the UI or by the existing `DailyPriceFetchBackgroundService`.

```csharp
// Query — no side effects
public async Task<PortfolioValuation> CalculateAsync(Guid accountId, CancellationToken ct)
{
    // Read-only: use existing prices, mark stale ones
}

// Command — separate handler
public class RefreshAccountPricesCommand : IRequest<Unit>
{
    public Guid AccountId { get; init; }
}
```

---

### 14. No IHttpClientFactory in Blazor WASM

**File:** `src/Privestio.Web/Program.cs` (line ~13)

**Problem:**
A raw `new HttpClient` is registered as scoped. No handler pipeline, no resilience policies, no centralized auth token injection. Every service manually reads from `AuthService` to add the token.

**Recommendation:**
Use `IHttpClientFactory` with a `DelegatingHandler`:

```csharp
// Program.cs
builder.Services.AddHttpClient("PrivestioApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
        ?? builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddTransient<AuthTokenHandler>();

// AuthTokenHandler.cs
public class AuthTokenHandler : DelegatingHandler
{
    private readonly AuthService _auth;

    public AuthTokenHandler(AuthService auth) => _auth = auth;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _auth.GetToken();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            _auth.Logout();

        return response;
    }
}
```

Update services to inject `IHttpClientFactory` and call `CreateClient("PrivestioApi")`.

---

### 15. No Code Coverage Collection in CI

**File:** `.github/workflows/ci.yml`

**Problem:**
`coverlet.collector` is installed in every test project but never invoked. No coverage reports are generated or tracked.

**Recommendation:**
```yaml
- name: Run Tests with Coverage
  run: |
    dotnet test tests/Privestio.Domain.Tests --no-build --verbosity normal \
      --collect:"XPlat Code Coverage" --results-directory ./coverage
    dotnet test tests/Privestio.Application.Tests --no-build --verbosity normal \
      --collect:"XPlat Code Coverage" --results-directory ./coverage
    dotnet test tests/Privestio.Infrastructure.Tests --no-build --verbosity normal \
      --collect:"XPlat Code Coverage" --results-directory ./coverage

- name: Generate Coverage Report
  uses: danielpalme/ReportGenerator-GitHub-Action@5
  with:
    reports: 'coverage/**/coverage.cobertura.xml'
    targetdir: 'coveragereport'
    reporttypes: 'HtmlSummary;Cobertura'

- name: Upload Coverage
  uses: actions/upload-artifact@v4
  with:
    name: coverage-report
    path: coveragereport
```

Consider adding a coverage threshold gate once a baseline is established.

---

### 16. No Rate Limiting on Reverse Proxy

**File:** `docker/Caddyfile`

**Problem:**
No `rate_limit` directive in Caddy. The API is exposed without throttling, making it vulnerable to abuse. Also no `request_body` size limit for upload endpoints.

**Recommendation:**
Add rate limiting via Caddy's `rate_limit` module (requires the `caddy-ratelimit` plugin) or implement it in the .NET application layer (see recommendation #4 for auth endpoints). For the application layer approach, add a global rate limiter in `Program.cs`:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
```

Add request body size limits for file upload endpoints:
```csharp
group.MapPost("/import", ...)
    .DisableAntiforgery()
    .Accepts<IFormFile>("multipart/form-data")
    .WithMetadata(new RequestSizeLimitAttribute(10 * 1024 * 1024)); // 10MB
```

---

### 17. No Docker Resource Limits or Log Rotation

**File:** `docker/docker-compose.yml`

**Problem:**
No memory/CPU limits on any service. Default `json-file` logging driver with no size cap. Logs will fill the disk; a runaway process consumes all host resources.

**Recommendation:**
```yaml
services:
  api:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 256M
    logging:
      driver: json-file
      options:
        max-size: "50m"
        max-file: "5"

  db:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
    logging:
      driver: json-file
      options:
        max-size: "50m"
        max-file: "5"
    # Add backup sidecar or cron job for pg_dump

  caddy:
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 256M
    logging:
      driver: json-file
      options:
        max-size: "20m"
        max-file: "3"
```

---

## P2 — Medium (Plan to Address)

---

### 18. Dual Timestamp Management

**Files:**
- `src/Privestio.Domain/Entities/BaseEntity.cs`
- `src/Privestio.Infrastructure/Data/PrivestioDbContext.cs`
- Various entity methods (e.g., `Account.Rename()`, `Security.UpdateCurrency()`)

**Problem:**
Domain methods set `UpdatedAt = DateTime.UtcNow`, and `DbContext.SaveChangesAsync` override also sets `UpdatedAt` on every modified entity. The domain-level assignment is effectively dead code.

**Recommendation:**
Remove all `UpdatedAt = DateTime.UtcNow` lines from domain entity methods. Let the DbContext hook be the single authority. Search for `UpdatedAt = DateTime` across all entity files and remove each occurrence.

Files to edit (search `UpdatedAt = DateTime.UtcNow` in `src/Privestio.Domain/`):
- All entity files that set `UpdatedAt` in their methods

---

### 19. DateTime.UtcNow Used Directly — Inject TimeProvider

**Files:** Throughout domain, application, and infrastructure layers

**Problem:**
Direct `DateTime.UtcNow` calls make time-dependent behavior untestable. Cannot write deterministic tests for expiry logic, scheduling, etc.

**Recommendation:**
Use .NET 8+'s built-in `TimeProvider`:

```csharp
// Registration in DI:
builder.Services.AddSingleton(TimeProvider.System);

// Inject in DbContext:
public class PrivestioDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly TimeProvider _timeProvider;

    public PrivestioDbContext(DbContextOptions options, TimeProvider timeProvider) : base(options)
    {
        _timeProvider = timeProvider;
    }

    // In SaveChangesAsync:
    entry.Entity.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
}

// In tests:
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
```

---

### 20. God-Object UnitOfWork (28 Repository Properties)

**Files:**
- `src/Privestio.Application/Interfaces/IUnitOfWork.cs`
- `src/Privestio.Infrastructure/Data/UnitOfWork.cs`

**Problem:**
The `IUnitOfWork` interface exposes 28 repository properties. Every consumer gets access to every repository. All repositories are eagerly instantiated in the constructor, even when a handler only uses 1-2.

**Recommendation:**
Phase 1 — Make repositories lazy:
```csharp
private readonly Lazy<IAccountRepository> _accounts;
public IAccountRepository Accounts => _accounts.Value;

public UnitOfWork(PrivestioDbContext context)
{
    _context = context;
    _accounts = new Lazy<IAccountRepository>(() => new AccountRepository(context));
    // ... repeat for all repositories
}
```

Phase 2 — Inject repositories directly into handlers (preferred long-term):
```csharp
// Instead of:
public CreateAccountCommandHandler(IUnitOfWork unitOfWork)

// Use:
public CreateAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork) // UoW only for SaveChangesAsync
```

This requires `IUnitOfWork` to be trimmed down to just `SaveChangesAsync` and `BeginTransactionAsync`.

---

### 21. Soft-Delete Query Filters Use Manual Entity List

**File:** `src/Privestio.Infrastructure/Data/PrivestioDbContext.cs`

**Problem:**
Each new entity must be manually added to the soft-delete filter configuration. Missing one silently leaks deleted records into queries.

**Recommendation:**
Use convention-based registration (same pattern already used for `Version`):
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrivestioDbContext).Assembly);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes()
        .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
    {
        // Concurrency token
        modelBuilder.Entity(entityType.ClrType)
            .Property(nameof(BaseEntity.Version))
            .IsConcurrencyToken();

        // Soft-delete filter (convention-based)
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var filter = Expression.Lambda(Expression.Not(property), parameter);
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
    }
}
```

Remove all the individual `.HasQueryFilter(e => !e.IsDeleted)` lines from entity configurations.

---

### 22. ImportTransactionsCommandHandler is Too Large

**File:** `src/Privestio.Application/Commands/ImportTransactions/ImportTransactionsCommandHandler.cs` (29KB, 600+ lines)

**Problem:**
Single class handles 6 responsibilities: parsing, fingerprinting, dedup, transaction creation, investment position upserts, and historical price fetching.

**Recommendation:**
Extract into focused services:

1. **`IImportOrchestrator`** — coordinates the flow (the handler becomes thin)
2. **`ITransactionDeduplicator`** — fingerprinting + duplicate detection (lines 176-220)
3. **`IImportedTransactionFactory`** — creates Transaction entities from parsed rows (lines 221-290)
4. **`IInvestmentPositionUpsertService`** — handles holding/lot/security creation (lines 458-587, the existing `UpsertInvestmentPositionsFromImportAsync`)
5. **`IImportPriceSyncService`** — fetches historical prices for imported securities (lines 359-439)

Also fix specific bugs:
- Line 323: Replace `catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")` with proper `catch (DbUpdateException ex)`
- Line 224: Decide if zero-amount transactions should be `Credit` or have a separate classification
- Extract `ImportResultResponse` construction into a private factory method (duplicated 4 times)

---

### 23. MemoryStream Leak in Import.razor

**File:** `src/Privestio.Web/Pages/Import.razor` (line 268)

**Problem:**
`_selectedFile` is a `MemoryStream` disposed only in `ResetImport()`. Navigating away without clicking "Import Another" leaks up to 10MB.

**Recommendation:**
Implement `IDisposable`:
```csharp
@implements IDisposable

@code {
    // ... existing code ...

    public void Dispose()
    {
        _selectedFile?.Dispose();
        _selectedFile = null;
    }
}
```

---

### 24. Missing Error Handling in Razor Pages

**Files:** `src/Privestio.Web/Pages/Import.razor` and other pages

**Problem:**
Async methods lack `try/catch`. If a service throws, `_isLoading` stays `true` forever and the UI freezes.

**Recommendation:**
Wrap all async operations with try/finally:
```csharp
private async Task PreviewFileAsync()
{
    _isLoading = true;
    _errorMessage = null;
    try
    {
        // existing logic
    }
    catch (Exception ex)
    {
        _errorMessage = "Failed to preview file. Please try again.";
        // Consider injecting ILogger for structured logging
    }
    finally
    {
        _isLoading = false;
        StateHasChanged();
    }
}
```

Apply this pattern to all `_isLoading`-gated methods across all pages. Priority pages:
- `Import.razor` (3 methods: `PreviewFileAsync`, `RunPreviewImportAsync`, `CommitImportAsync`)
- `Securities.razor`
- `SecurityReview.razor`
- `TransactionDetail.razor`
- `Reconciliation.razor`

---

### 25. No CancellationToken Propagation in Frontend

**Files:** All web services in `src/Privestio.Web/Services/`

**Problem:**
No service method accepts a `CancellationToken`. Navigating away doesn't cancel in-flight HTTP requests.

**Recommendation:**
Add `CancellationToken` to all service methods:
```csharp
// Before:
public async Task<AccountResponse?> GetAccountAsync(Guid id)

// After:
public async Task<AccountResponse?> GetAccountAsync(Guid id, CancellationToken ct = default)
{
    var response = await _http.GetAsync($"api/v1/accounts/{id}", ct);
    // ...
}
```

In Razor pages, use `CancellationTokenSource` tied to component lifecycle:
```csharp
@implements IDisposable

@code {
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        _data = await Service.GetDataAsync(_cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
```

---

### 26. Hardcoded Provider Logic in Domain Service

**File:** `src/Privestio.Application/Services/SecurityResolutionService.cs` (lines 12-26, 345-401)

**Problem:**
Yahoo Finance-specific symbol formatting (`KnownExchangeSuffixes`, `.TO` suffix logic) and a `switch` on `"YahooFinance"` string literal are embedded in the domain resolution service. Adding a new provider requires modifying this service.

**Recommendation:**
Move provider-specific symbol formatting into each provider:
```csharp
// In IPriceFeedProvider or IPriceSourcePlugin, add:
string NormalizeSymbolForProvider(string rawSymbol, string? exchange);

// In SecurityResolutionService, replace the switch:
var provider = _pluginRegistry.GetProvider(providerName);
var normalizedSymbol = provider.NormalizeSymbolForProvider(symbol, exchange);
```

Move `KnownExchangeSuffixes` into a shared configuration or the Yahoo provider module.

---

### 27. No Static Analysis or Vulnerability Scanning in CI

**File:** `.github/workflows/ci.yml`

**Recommendation:**
Add these steps:
```yaml
- name: Check Code Formatting
  run: dotnet format --verify-no-changes --verbosity diagnostic

- name: Check for Vulnerable Packages
  run: dotnet list package --vulnerable --include-transitive 2>&1 | tee vulnerability-report.txt

- name: Fail on Vulnerabilities
  run: |
    if grep -q "has the following vulnerable packages" vulnerability-report.txt; then
      echo "Vulnerable packages detected!"
      exit 1
    fi
```

Consider adding Roslyn analyzers via `Directory.Build.props`:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" />
  <PackageReference Include="SonarAnalyzer.CSharp" Version="10.0.0" />
</ItemGroup>
```

---

### 28. Ambiguous Security Resolution Silently Picks First Match

**File:** `src/Privestio.Application/Services/SecurityResolutionService.cs` (lines 112-122)

**Problem:**
When multiple securities match a symbol-only lookup, the code logs a warning but returns `candidates[0]`. This could bind transactions to the wrong security.

**Recommendation:**
Return a conflict result instead of silently picking:
```csharp
if (candidates.Count > 1)
{
    _logger.LogWarning("Ambiguous match for symbol {Symbol}: {Count} candidates", symbol, candidates.Count);
    return new SecurityResolutionResult
    {
        Status = ResolutionStatus.Ambiguous,
        Candidates = candidates
    };
}
```

The caller (import handler) should then surface this as a user-resolvable conflict.

---

### 29. Missing Validator Tests

**Files:**
- `tests/Privestio.Application.Tests/Validators/` — only `CreateAccountCommandValidatorTests.cs` exists
- Missing: `CreateBudgetCommandValidatorTests`, `CreateRecurringTransactionCommandValidatorTests`, `CreateSinkingFundCommandValidatorTests`

**Recommendation:**
Add tests for each validator covering:
- Valid input passes
- Each validation rule failure (empty name, negative amount, invalid date range, etc.)
- Boundary values (max length, min/max amounts)
- Null/empty required fields

---

### 30. Auto-Migration in Production

**File:** `src/Privestio.Infrastructure/DependencyInjection.cs` (lines 117-122)

**Problem:**
`MigrateAsync()` runs unconditionally on startup. The runtime connection needs DDL-level permissions (`ALTER TABLE`, `CREATE TABLE`). A bad migration in production can take down the entire application.

**Recommendation:**
1. Gate auto-migration to development only:
```csharp
public static async Task ApplyMigrationsAsync(this IServiceProvider services)
{
    var env = services.GetRequiredService<IHostEnvironment>();
    if (!env.IsDevelopment())
    {
        // In production, migrations are applied by CI/CD
        return;
    }

    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PrivestioDbContext>();
    await context.Database.MigrateAsync();
}
```

2. Add a migration step to the CI/CD pipeline:
```yaml
- name: Apply Migrations
  run: dotnet ef database update --project src/Privestio.Infrastructure --startup-project src/Privestio.Api
  env:
    ConnectionStrings__PrivestioDb: ${{ secrets.DB_CONNECTION_STRING }}
```

3. Use a separate connection string with DDL permissions for migrations only.

---

## P3 — Low (Nice to Have)

---

### 31. Monolithic CSS File

**File:** `src/Privestio.Web/wwwroot/css/app.css` (~1342 lines)

**Recommendation:** Split into component-scoped `.razor.css` files for components that have unique styles. Keep `app.css` for global reset, variables, and layout primitives only.

---

### 32. Inline Styles Bypass Theme System

**File:** `src/Privestio.Web/Pages/Dashboard.razor`

**Recommendation:** Replace hardcoded hex colors (`#107c10`, `#c50f1f`) with CSS variables (`var(--success-soft)`, `var(--danger-soft)`).

---

### 33. Flat Service Registrations in Web Program.cs

**File:** `src/Privestio.Web/Program.cs` (28 `AddScoped` calls)

**Recommendation:** Group into extension methods:
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFinanceWebServices(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddScoped<TransactionService>();
        // ...
        return services;
    }
}
```

---

### 34. ErrorHandlingExtensions Leaks Internal Details

**File:** `src/Privestio.Api/Middleware/ErrorHandlingExtensions.cs` (line 42)

**Recommendation:** Replace `KeyNotFoundException.Message` with a generic message:
```csharp
catch (KeyNotFoundException)
{
    return Results.Problem(
        detail: "The requested resource was not found.",
        statusCode: StatusCodes.Status404NotFound);
}
```

---

### 35. Application Services Registered as Concrete Types

**File:** `src/Privestio.Application/DependencyInjection.cs`

**Recommendation:** Extract interfaces for `NotificationService`, `SecurityResolutionService`, `HistoricalValueTimelineService`, `InvestmentPortfolioValuationService`, `PortfolioPerformanceCalculator`, and `TransactionFingerprintService`. Register as `AddScoped<INotificationService, NotificationService>()`.

---

### 36. No Docker Image Pinning

**File:** `docker/Dockerfile.api`

**Recommendation:** Pin base images by digest:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:<digest> AS build
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:<digest> AS final
```
Verify `.dockerignore` exists and excludes `bin/`, `obj/`, `.git/`.

---

### 37. Email Auto-Confirmed on Registration

**File:** `src/Privestio.Api/Endpoints/AuthEndpoints.cs` (line 54)

**Recommendation:** Implement email verification when SMTP is available. Until then, document the risk in a code comment.

---

### 38. Hardcoded E2E Test Password

**File:** `tests/Privestio.E2E.Tests/PlaywrightTestBase.cs` (line 80)

**Recommendation:** Source from environment variable with a fallback for local dev:
```csharp
private string TestPassword => Environment.GetEnvironmentVariable("E2E_TEST_PASSWORD") ?? "Admin@Privestio123!";
```

---

### 39. No Screenshot-on-Failure in E2E Tests

**File:** `tests/Privestio.E2E.Tests/PlaywrightTestBase.cs`

**Recommendation:** Capture screenshots in a test teardown:
```csharp
public async Task TearDown([CallerMemberName] string testName = "")
{
    if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
    {
        await Page.ScreenshotAsync(new() { Path = $"screenshots/{testName}.png" });
    }
}
```

---

### 40. CORS Allows Any Header and Method

**File:** `src/Privestio.Api/Program.cs` (line 176)

**Recommendation:**
```csharp
policy.WithOrigins(allowedOrigin)
    .WithHeaders("Authorization", "Content-Type", "Accept")
    .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH");
```

---

## Implementation Order

Recommended implementation sequence:

| Phase | Items | Rationale |
|-------|-------|-----------|
| **Phase 1: Security** | #2, #3, #4, #40 | Auth hardening — highest user risk |
| **Phase 2: Data Integrity** | #1, #8, #9 | Financial data correctness |
| **Phase 3: CI/CD** | #5, #6, #7, #15, #27 | Automated quality gates |
| **Phase 4: API Quality** | #10, #11, #14, #16, #34 | Client reliability and consistency |
| **Phase 5: Performance** | #12, #13, #22 | N+1 queries and CQRS compliance |
| **Phase 6: Code Quality** | #17, #18, #19, #20, #21, #23, #24, #25, #30 | Maintainability and operational safety |
| **Phase 7: Polish** | #26, #28, #29, #31-39 | Extensibility, testing, deployment |
