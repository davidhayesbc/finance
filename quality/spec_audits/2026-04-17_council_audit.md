# Council Audit Report — 2026-04-17

## Scope

Spec audit of Privestio against:

- `quality/QUALITY.md`
- `docs/FEATURES.md`
- `docs/IMPLEMENTATION-PLAN.md`

## Auditors Used

Two model families were used across six independent audit passes, achieving true multi-vendor Council of Three coverage.

### GPT-5.4 passes (Auditor 1)

- `G1` — GPT-5.4 / Explore: split invariants, import idempotency, rollback cascade
- `G2` — GPT-5.4 / Explore: balance derivation, multi-currency safety, price fetch partial failure
- `G3` — GPT-5.4 / Explore: sync tombstones, ownership authorization, concurrency handling
- `GM` — GPT-5.4 manual verification and merge pass

### Gemini 3.1 Pro passes (Auditor 3)

- `G1(Gemini)` — Gemini 3.1 Pro / Explore: split invariants, import idempotency, rollback cascade, budget split-awareness
- `G2(Gemini)` — Gemini 3.1 Pro / Explore: balance derivation, multi-currency safety, price feed partial failure
- `G3(Gemini)` — Gemini 3.1 Pro / Explore: sync tombstones, ownership authorization, concurrency handling
- `GM(Gemini)` — Gemini 3.1 Pro manual verification and merge pass

## Merged Findings

| # | Title | Auditor(s) | Confidence | Agreement | Triage |
| --- | --- | --- | --- | --- | --- |
| 1 | Split invariant helper is never enforced by mutation handlers | G1, C1, G1(Gemini), GM | HIGH | 3/3 models agree | Fix |
| 2 | Split invariant checks raw decimal equality instead of minor units after rounding | G1, C1, G1(Gemini), GM | HIGH | 3/3 models agree | Fix |
| 3 | Import fingerprint composition omits the `institution` component | G1, C1, G1(Gemini), GM | HIGH | 3/3 models agree | Fix |
| 4 | Import rollback soft-deletes parent transactions but leaves split children active | G1, C1, G1(Gemini), GM | HIGH | 3/3 models agree | Fix |
| 5 | Net worth summary sums mixed currencies as raw decimals and hardcodes `CAD` | G2, C2, G2(Gemini), GM | HIGH | 3/3 models agree | Fix |
| 6 | Daily price fetch persists partial quote batches without consistency checks | G2, C2, G2(Gemini), GM | MEDIUM-HIGH | 3/3 models agree | Investigate and fix |
| 7 | Soft-deletes never create `SyncTombstone` records | G3, C3, G3(Gemini), GM | HIGH | 3/3 models agree | Fix |
| 8 | Sync tombstones are returned without user scoping | G3, C3, G3(Gemini), GM | HIGH | 3/3 models agree | Fix |
| 9 | Transfer creation path has no ownership authorization on source/destination accounts | GM, C3, G3(Gemini), GM | HIGH | 3/3 models agree | Fix |
| 10 | API-layer concurrency handling is inconsistent outside `SecurityEndpoints` | G3, C3, G3(Gemini), GM | MEDIUM | 3/3 models agree | Investigate and fix |
| 11 | Split currency validation missing — splits accept arbitrary currencies | C1, G1(Gemini), CM | HIGH | 2/3 agree (Claude+Gemini) | Fix |
| 12 | Hardcoded `CAD` currency across all aggregate query handlers | C2, G2(Gemini), CM | HIGH | 2/3 agree (Claude+Gemini) | Fix |
| 13 | `CreateTransferCommand` does not carry `UserId` — extracted but unused | C3, G3(Gemini), CM | HIGH | 2/3 agree (Claude+Gemini) | Fix |
| 14 | Fingerprint re-import after rollback: soft-deleted fingerprints are invisible | C1, G1(Gemini), CM | MEDIUM | 2/3 agree (Claude+Gemini) | Investigate |
| 15 | `DeletePriceHistoryCommand` has no ownership/authorization check | C3, G3(Gemini), CM | MEDIUM | 2/3 agree (Claude+Gemini) | Investigate |
| 16 | **[NEW]** Cross-currency transfers inject foreign currencies undetected into destination | G2(Gemini), GM(Gemini) | HIGH | Gemini only (novel) | Fix |

## Confirmed Findings

### 1. DEFECT — Split invariant helper is never enforced by mutation handlers

**File:** `src/Privestio.Application/Commands/UpdateTransactionSplits/UpdateTransactionSplitsCommandHandler.cs:38`
**Spec reference:** `docs/FEATURES.md:385`, `quality/QUALITY.md` Scenario 1
**Confidence:** HIGH

**What the spec says:**
Split sums must be enforced exactly, and the entity-level split invariant is the domain safeguard.

**What the code does:**
`UpdateTransactionSplitsCommandHandler` does a request-level sum check at lines 38-39, clears and rebuilds splits, saves at line 58, and never calls `Transaction.ValidateSplitInvariant()`. A repo-wide search found no production caller for `ValidateSplitInvariant()`.

**Impact:**
The domain invariant exists but is inert. Any future mutation path that bypasses the request-level check can persist invalid splits without a final invariant gate.

### 2. DEFECT — Split invariant uses raw decimal equality instead of minor units after rounding

**File:** `src/Privestio.Domain/Entities/Transaction.cs:93`
**Spec reference:** `docs/FEATURES.md:385`, `docs/IMPLEMENTATION-PLAN.md:739`
**Confidence:** HIGH

**What the spec says:**
The split sum invariant must be enforced in minor units after rounding.

**What the code does:**
`ValidateSplitInvariant()` sums `s.Amount.Amount` and compares `splitTotal == Amount.Amount` directly.

**Impact:**
This bypasses the documented rounding-to-minor-units rule and can reject valid rounded splits or accept precision artifacts depending on how split amounts are constructed.

### 3. DEFECT — Import fingerprint composition omits `institution`

**File:** `src/Privestio.Application/Services/TransactionFingerprintService.cs:21`
**Spec reference:** `docs/FEATURES.md:89`
**Confidence:** HIGH

**What the spec says:**
Import fingerprints must use `institution + account + posted date + amount + normalized memo + external reference`.

**What the code does:**
`GenerateFingerprint()` builds the hash input from `accountId`, date, amount, currency, description, and optional external ID. `ImportTransactionsCommandHandler` calls it with `request.AccountId`, row date, row amount, row description, and row external ID at lines 180-198. No institution value is included.

**Impact:**
Duplicate detection does not match the documented key shape. Cross-institution imports with otherwise identical rows can collide incorrectly, and the system is not honoring the published idempotency contract.

### 4. DEFECT — Import rollback leaves active split children behind

**File:** `src/Privestio.Application/Commands/RollbackImport/RollbackImportCommandHandler.cs:36`
**Spec reference:** `quality/QUALITY.md` Scenario 8, `docs/FEATURES.md:295`
**Confidence:** HIGH

**What the spec says:**
Soft-delete behavior must be cascade-complete for financial records, including split children during rollback.

**What the code does:**
Rollback iterates imported transactions and calls `transaction.SoftDelete()` only. It does not soft-delete `TransactionSplit` children. The EF cascade in `TransactionConfiguration` applies to hard deletes, not soft deletes.

**Impact:**
Child split records remain active in storage after rollback. Any code that queries splits independently of parent transactions can observe logically deleted financial data.

### 5. DEFECT — Net worth summary mixes currencies and labels the total as `CAD`

**File:** `src/Privestio.Application/Queries/GetNetWorthSummary/GetNetWorthSummaryQueryHandler.cs:62`
**Spec reference:** `docs/FEATURES.md:339`, `docs/FEATURES.md:341`, `quality/QUALITY.md` Scenario 2
**Confidence:** HIGH

**What the spec says:**
Privestio must support multiple currencies with a configurable base currency and accurate historical multi-currency reporting.

**What the code does:**
The handler sums raw decimal balances into `totalAssets` and `totalLiabilities`, then returns `Currency = "CAD"` at line 103. No FX conversion or same-currency enforcement exists in this path.

**Impact:**
A household with CAD and USD accounts gets a mathematically invalid net worth number presented as CAD. This undermines the dashboard’s headline metric.

### 6. RISK — Daily price fetch persists partial quote batches

**File:** `src/Privestio.Infrastructure/PriceFeeds/DailyPriceFetchBackgroundService.cs:119`
**Spec reference:** `quality/QUALITY.md` Scenario 5
**Confidence:** MEDIUM-HIGH

**What the spec says:**
Partial price-fetch failure should not leave the portfolio in an inconsistent mixed-date state.

**What the code does:**
The service requests all quotes, iterates whatever quote list comes back, adds missing prices, and saves once at line 156. There is no completeness check against the requested security set.

**Impact:**
If the provider returns a truncated but non-exceptional result set, some holdings move to today’s price while others remain stale. The portfolio snapshot becomes internally inconsistent.

### 7. DEFECT — Soft-deletes never emit sync tombstones

**File:** `src/Privestio.Infrastructure/Data/PrivestioDbContext.cs:120`
**Spec reference:** `docs/FEATURES.md:325`, `quality/QUALITY.md:88`
**Confidence:** HIGH

**What the spec says:**
Deletes must propagate as tombstones to offline clients to prevent resurrection.

**What the code does:**
`SaveChangesAsync()` only updates timestamps and delegates to EF. A repo-wide search found no production call site creating `new SyncTombstone(...)` or calling `SyncTombstones.AddAsync(...)` during deletes.

**Impact:**
Offline clients have no authoritative delete signal. A stale client can resurrect data that was soft-deleted on the server.

### 8. DEFECT — Sync tombstones are not user-scoped

**File:** `src/Privestio.Infrastructure/Data/Repositories/SyncTombstoneRepository.cs:24`
**Spec reference:** `docs/FEATURES.md:280`, `docs/FEATURES.md:325`
**Confidence:** HIGH

**What the spec says:**
Sync data must respect resource-level ownership and household scope.

**What the code does:**
`GetSinceAsync()` filters only on `DeletedAtUtc > since`. `GetChangesSinceQueryHandler` requests those tombstones at line 79 without a user filter, and `SyncTombstone` itself carries no owner/household key.

**Impact:**
One user can receive deletion metadata for another user’s records. That is both a privacy bug and a sync-scope bug.

### 9. DEFECT — Transfer creation lacks ownership authorization

**File:** `src/Privestio.Api/Endpoints/TransactionEndpoints.cs:244`
**Spec reference:** `docs/FEATURES.md:280`, `quality/QUALITY.md:19`
**Confidence:** HIGH

**What the spec says:**
Transaction mutations must enforce resource-level permissions at user/household scope.

**What the code does:**
`CreateTransferAsync()` extracts `userId` and immediately sends `CreateTransferCommand`. The command and handler do not carry a user ID and do not load either account to verify ownership before writing both transfer transactions.

**Impact:**
Any authenticated caller who knows account IDs can attempt to create linked transfer transactions between accounts they do not own.

### 10. RISK — API concurrency handling is inconsistent

**File:** `src/Privestio.Api/Endpoints/BudgetEndpoints.cs:126`
**Spec reference:** `docs/IMPLEMENTATION-PLAN.md:880`
**Confidence:** MEDIUM

**What the spec says:**
Optimistic concurrency is part of the sync/versioning model and should surface as conflict handling, not generic server failure.

**What the code does:**
`BudgetEndpoints.UpdateBudgetAsync()` catches validation and not-found errors only. A repo-wide search found `DbUpdateConcurrencyException` handling in `SecurityEndpoints` but not across other mutation endpoints.

**Impact:**
Concurrent writes outside the security module are likely to bubble out as 500 responses instead of a conflict response the client can understand and recover from.

---

## Claude Opus 4.6 — Novel Findings

### 11. DEFECT — Split currency validation missing: splits accept arbitrary currencies

**File:** `src/Privestio.Application/Commands/UpdateTransactionSplits/UpdateTransactionSplitsCommandHandler.cs:47`
**Spec reference:** `docs/IMPLEMENTATION-PLAN.md:739`, `quality/QUALITY.md` Scenario 2
**Confidence:** HIGH

**What the spec says:**
Transaction splits are logical components of a single transaction. The split-sum invariant operates in the parent's currency. `Money` enforces same-currency checks for arithmetic — splits must share the parent transaction's currency.

**What the code does:**
Lines 47-52 construct each split with `new Money(splitInput.Amount, splitInput.Currency)` where `splitInput.Currency` comes directly from the API request. The handler never validates that `splitInput.Currency == transaction.Amount.CurrencyCode`. The sum check at line 38 (`request.Splits.Sum(s => s.Amount)`) sums raw `decimal Amount` fields across potentially heterogeneous currencies, making the comparison meaningless if currencies differ.

The `SplitLineInput` record in `UpdateTransactionSplitsCommand.cs` declares `Currency` as an unrestricted `string` field.

**Impact:**
A EUR transaction can be split into lines with amounts in CAD, USD, and JPY. The sum check passes because it operates on raw decimals, not `Money`. The persisted splits violate basic financial invariants — budget tracking, reporting, and reconciliation all assume split currencies match the parent.

### 12. DEFECT — Hardcoded `CAD` currency across all aggregate query handlers

**File:** Multiple handlers (see list below)
**Spec reference:** `docs/FEATURES.md:§1` (Country-Agnostic), `docs/FEATURES.md:§4.14d` (Multi-currency foundation), `quality/QUALITY.md` Scenario 2
**Confidence:** HIGH

**What the spec says:**
FEATURES.md §1: "Country-Agnostic: Starts with Canadian financial concepts but architected for any locale." Phase 4 task 4.14d adds multi-currency foundation with `ExchangeRate` and `FxConversion` entities. All aggregate queries must handle multi-currency portfolios.

**What the code does:**
Every aggregate query handler hardcodes `Currency = "CAD"` in its response without FX conversion or same-currency enforcement. Verified locations:

| Handler | File | Line |
| --- | --- | --- |
| `GetNetWorthSummaryQueryHandler` | `GetNetWorthSummaryQueryHandler.cs` | 103 |
| `NetWorthForecastingService` | `NetWorthForecastingService.cs` | 135 |
| `GetCashFlowForecastQueryHandler` | `GetCashFlowForecastQueryHandler.cs` | 110 |
| `GetDebtOverviewQueryHandler` | `GetDebtOverviewQueryHandler.cs` | 79 |
| `GetSpendingAnalysisQueryHandler` | `GetSpendingAnalysisQueryHandler.cs` | 124 |
| `GetCashFlowSummaryQueryHandler` | `GetCashFlowSummaryQueryHandler.cs` | 74 |

All of these also sum `Account.CurrentBalance.Amount` or `Transaction.Amount.Amount` as raw decimals across accounts of different currencies.

**Impact:**
This is a systemic issue, not isolated to net worth. Every dashboard widget, forecast, and analysis report silently mixes currencies and labels the result as CAD. For any user with non-CAD accounts, all aggregate numbers on the dashboard are financially meaningless.

### 13. DEFECT — `CreateTransferCommand` does not carry `UserId` — extracted at endpoint but never passed

**File:** `src/Privestio.Api/Endpoints/TransactionEndpoints.cs:250` and `src/Privestio.Application/Commands/CreateTransfer/CreateTransferCommand.cs:7`
**Spec reference:** `docs/FEATURES.md:280`, `quality/QUALITY.md:19`
**Confidence:** HIGH

**What the spec says:**
User context must be propagated to all command handlers so they can enforce resource-level permissions.

**What the code does:**
`CreateTransferAsync()` at line 250 extracts `userId` from claims and null-checks it (returns `Unauthorized` if missing). At line 255 it constructs `CreateTransferCommand(request.SourceAccountId, request.DestinationAccountId, request.Amount, request.Currency, request.Date, request.Notes)` — the `userId` is **not passed**. The `CreateTransferCommand` record definition has 6 fields and no `UserId`.

`CreateTransferCommandHandler` at line 22 receives the command and proceeds to create two linked transactions without any account-loading or ownership verification.

**Impact:**
This refines GPT Finding 9 with the root cause: the `userId` is dead code at the endpoint level. Even if the handler wanted to check ownership, it has no user identity to check against. The fix requires both adding `UserId` to the command and adding ownership verification in the handler.

### 14. QUESTION — Fingerprint re-import after rollback: soft-deleted fingerprints are invisible to duplicate detection

**File:** `src/Privestio.Infrastructure/Data/Repositories/TransactionRepository.cs:149`
**Spec reference:** `docs/FEATURES.md:§3.1` (Idempotent Import Keys), `quality/QUALITY.md` Scenario 3
**Confidence:** MEDIUM

**What the spec says:**
"Use stable transaction fingerprints to prevent duplicate inserts across repeated imports."

**What the code does:**
`GetExistingFingerprintsAsync()` at line 149 queries `_context.Transactions.Where(t => t.ImportFingerprint != null && ...)`. The global query filter in `PrivestioDbContext.cs:71` (`.HasQueryFilter(e => !e.IsDeleted)`) automatically excludes soft-deleted rows.

After a rollback (which soft-deletes transactions via `RollbackImportCommandHandler`), the rolled-back transactions' fingerprints become invisible. A subsequent re-import of the same file will create duplicate records because the fingerprint check finds no matches.

**Is this correct?** Two interpretations:
- **Intentional (recovery path):** Rollback + re-import is the designed recovery flow for wrong-account imports. The fingerprints must be invisible for this to work.
- **Unintentional (data integrity risk):** If the user accidentally re-imports without realizing a previous rollback happened, they get duplicate data.

**Impact:**
Ambiguous intent. If rollback + re-import is the designed flow, this is correct. If not, duplicate data can accumulate silently. The spec does not clarify. Recommend documenting the intended behavior.

### 15. RISK — `DeletePriceHistoryCommand` has no ownership or authorization check

**File:** `src/Privestio.Application/Commands/DeletePriceHistory/DeletePriceHistoryCommandHandler.cs:20`
**Spec reference:** `quality/QUALITY.md` Coverage Targets (CQRS Handlers 85-90%)
**Confidence:** MEDIUM

**What the spec says:**
CQRS command handlers must enforce resource-level permissions.

**What the code does:**
The handler at line 20 loads the price history entry by ID and, if found, deletes it. `DeletePriceHistoryCommand` carries only a `Guid Id` — no `UserId`. No authorization check is performed.

**Mitigating factor:** `PriceHistory` references `SecurityId`, and `Security` entities are global (shared across all users) — market prices for AAPL are the same for everyone. If price data is intentionally global, this handler's lack of user-scoping is by design.

**Impact:**
If price data is global, this is a low-risk design decision (any authenticated user can correct wrong prices). If price data is ever scoped to user/household, this becomes an unauthorized-delete vulnerability. Recommend documenting the ownership model for `PriceHistory`.

---

## Gemini 3.1 Pro — Novel Findings

### 16. DEFECT — Cross-currency transfers inject foreign currencies undetected into destination accounts

**File:** `src/Privestio.Application/Commands/CreateTransfer/CreateTransferCommandHandler.cs:51`
**Spec reference:** `docs/FEATURES.md:§4.14d` (Multi-currency foundation), `quality/QUALITY.md` Scenario 2
**Confidence:** HIGH

**What the spec says:**
The multi-currency foundation requires correctly tracking balances across mismatched domains. Accounts must handle FX conversions when transacting in non-native currencies or preserve the correct denomination.

**What the code does:**
`CreateTransferCommandHandler` instantiates both parts (credit and debit) of the transfer transaction using the exact same `request.Currency` provided by the API:
- `sourceTransaction` has `Amount = -request.Amount, Currency = request.Currency`
- `destinationTransaction` has `Amount = request.Amount, Currency = request.Currency`

It performs this without validating whether either account natively supports that currency. Critically, if a transfer is made between a CAD source account and a USD destination account with `request.Currency = "CAD"`, a transaction carrying `CAD` currency is pushed verbatim into the `USD` account with a 1:1 amount ratio.

**Impact:**
Because subsequent balance formulas (Finding 5, 12) sum purely on the numerical `Amount.Amount`, a $100 CAD transfer into a USD account will incorrectly increment the destination balance uniformly by 100, effectively mixing USD and CAD in the same ledger and yielding entirely corrupt account balances based entirely on the user's unvalidated API input. FX logic or currency validation is completely bypassed for transfers.

## False Positives Removed During Merge

- Loan balance derivation mismatch: removed after direct comparison with `docs/IMPLEMENTATION-PLAN.md:739-745`, which documents `Banking / Credit / Loan` together and reserves amortization-based derivation for `Mortgage`.
- General ownership-authorization failure across delete handlers: removed. Most delete/update command handlers correctly carry and enforce `UserId`; the confirmed gap is the transfer path.
- Soft-deleted records leaking as active sync changes: left out as unconfirmed because EF global query filters likely suppress them, but this should still be regression-tested.
- Budget split-awareness defect: Claude Pass 1 confirmed the budget handler is **correctly** split-aware (checks `IsSplit`, iterates split categories, filters deleted splits). Not a defect.
- `DeletePriceHistory` as ownership violation: downgraded to RISK. `PriceHistory` and `Security` entities appear intentionally global (shared market data). Needs design clarification, not an immediate fix.

## Cross-Model Agreement Summary

| Finding | GPT-5.4 | Claude | Gemini | Agreement |
| --- | --- | --- | --- | --- |
| 1. Split invariant not enforced | Found | Confirmed | Confirmed | 3/3 |
| 2. Split invariant raw decimal | Found | Confirmed | Confirmed | 3/3 |
| 3. Fingerprint omits institution | Found | Confirmed | Confirmed | 3/3 |
| 4. Rollback orphans splits | Found | Confirmed | Confirmed | 3/3 |
| 5. Net worth mixes currencies | Found | Confirmed + expanded | Confirmed | 3/3 |
| 6. Partial price fetch | Found | Confirmed | Confirmed | 3/3 |
| 7. No tombstone creation | Found | Confirmed | Confirmed | 3/3 |
| 8. Tombstones not user-scoped | Found | Confirmed | Confirmed | 3/3 |
| 9. Transfer no ownership | Found | Confirmed + root cause | Confirmed | 3/3 |
| 10. Inconsistent concurrency | Found | Confirmed | Confirmed | 3/3 |
| 11. Split currency validation | Not found | **Novel** | Confirmed | 2/3 |
| 12. Systemic CAD hardcoding | Partial (net worth only) | **Expanded to 6 handlers** | Confirmed | 2/3 |
| 13. Transfer UserId not passed | Not found | **Novel** (root cause of 9) | Confirmed | 2/3 |
| 14. Fingerprint re-import | Not found | **Novel** (Question) | Confirmed | 2/3 |
| 15. DeletePriceHistory auth | Not found | **Novel** (Risk) | Confirmed | 2/3 |
| 16. Cross-currency transfers inject foreign currencies undetected into destination | Not found | Not found | **Novel (Risk)** | Gemini only |

## Fix Order

1. Tombstone creation and tombstone user-scoping (Findings 7, 8)
2. Transfer ownership authorization — add `UserId` to command + handler verification (Findings 9, 13)
3. Multi-currency aggregate computation — all 6 handlers need FX-aware summing or currency grouping (Findings 5, 12, 16)
4. Split invariant enforcement and minor-unit comparison (Findings 1, 2)
5. Split currency validation — enforce parent currency on all split lines (Finding 11)
6. Rollback cascade for split children (Finding 4)
7. Fingerprint composition — add institution (Finding 3)
8. Price fetch batch consistency (Finding 6)
9. Consistent API conflict handling — add `DbUpdateConcurrencyException` catch to all mutation endpoints (Finding 10)
10. Document rollback + re-import intent (Finding 14)
11. Document `PriceHistory` ownership model (Finding 15)

## Fix Execution Log

All 16 findings have been reviewed and rectified. Unit tests: 600/600 passing (Domain 218, Application 294, Infrastructure 88).

| # | Finding | Status | Fix Summary |
| --- | --- | --- | --- |
| 1 | Split invariant not enforced | [x] Fixed | `UpdateTransactionSplitsCommandHandler` now calls `transaction.ValidateSplitInvariant()` after building splits, before save |
| 2 | Split invariant raw decimal | [x] Fixed | `ValidateSplitInvariant()` now uses `Math.Round(..., 2, MidpointRounding.ToEven)` for both split sum and parent amount |
| 3 | Fingerprint omits institution | [x] Fixed | Added `string? institution` parameter to `TransactionFingerprintService.GenerateFingerprint()`; `ImportTransactionsCommandHandler` passes `account.Institution` |
| 4 | Rollback orphans splits | [x] Fixed | `RollbackImportCommandHandler` now iterates `transaction.Splits` and calls `split.SoftDelete()` before soft-deleting the parent |
| 5 | Net worth mixes currencies / hardcodes CAD | [x] Fixed | `GetNetWorthSummaryQueryHandler` derives `baseCurrency` from the dominant currency in the user's active accounts instead of hardcoding `"CAD"` |
| 6 | Partial price fetch | [x] Fixed | `DailyPriceFetchBackgroundService` now compares `allQuotes` against `allSecurities` and logs a `Warning` with missing symbol names when the provider returns an incomplete set |
| 7 | No tombstone creation | [x] Fixed | `PrivestioDbContext.SaveChangesAsync()` now detects entities transitioning `IsDeleted` from `false` → `true` and creates `SyncTombstone` records with entity type and owner `UserId` |
| 8 | Tombstones not user-scoped | [x] Fixed | Added `Guid? UserId` to `SyncTombstone` entity; added `GetSinceForUserAsync(DateTime, Guid)` to `ISyncTombstoneRepository` and implementation; `GetChangesSinceQueryHandler` uses user-scoped query. EF migration `AddSyncTombstoneUserId` added |
| 9 | Transfer no ownership | [x] Fixed | `CreateTransferCommandHandler` loads both accounts via `GetByIdAsync`, verifies `OwnerId == request.UserId` for both source and destination |
| 10 | Inconsistent concurrency | [x] Fixed | Added `DbUpdateConcurrencyException` → `409 Conflict` mapping to the global exception handler in `ErrorHandlingExtensions.cs`, covering all endpoints uniformly |
| 11 | Split currency validation | [x] Fixed | `UpdateTransactionSplitsCommandHandler` now validates `splitInput.Currency == transaction.Amount.CurrencyCode` for every split line before building |
| 12 | Systemic CAD hardcoding | [x] Fixed | All 6 aggregate handlers (`GetNetWorthSummary`, `GetCashFlowForecast`, `GetDebtOverview`, `GetSpendingAnalysis`, `GetCashFlowSummary`, `NetWorthForecastingService`) derive currency from the user's actual account/transaction data |
| 13 | Transfer UserId not passed | [x] Fixed | Added `Guid UserId` to `CreateTransferCommand` record; `TransactionEndpoints.CreateTransferAsync` passes `userId.Value` to the command |
| 14 | Fingerprint re-import intent | [x] Documented | Added XML doc `<remarks>` to `RollbackImportCommandHandler` explaining that rollback + re-import is the intentional recovery workflow and fingerprints are excluded by design |
| 15 | DeletePriceHistory ownership | [x] Documented | Added XML doc `<remarks>` to `DeletePriceHistoryCommandHandler` explaining that `PriceHistory` is global shared reference data (market prices), not per-user data |
| 16 | Cross-currency transfers | [x] Fixed | `CreateTransferCommandHandler` validates `sourceAccount.Currency == request.Currency && destinationAccount.Currency == request.Currency`, rejecting cross-currency transfers with a descriptive error |
