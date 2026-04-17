# Quality Constitution: Privestio

## Purpose

Privestio is an offline-first, self-hosted personal finance tracker that manages users' real money — their bank accounts, investments, mortgages, budgets, and net worth calculations. "Working correctly" means more than passing tests: it means every balance is accurate to the cent, every import is idempotent, every sync conflict preserves data, and no silent corruption can masquerade as a valid financial state.

**Deming** ("quality is built in, not inspected in") — Quality is built into this quality playbook, the AGENTS.md bootstrap file, and the functional test suite so that every AI session inherits the same bar. A new contributor reads QUALITY.md before writing code; the bar is inherited, not re-discovered.

**Juran** ("fitness for use") — Fitness for Privestio means: financial calculations are correct to the cent across all account types and currencies; file imports never create duplicate transactions or lose rows silently; offline mutations sync without data loss; and the user can trust their dashboard numbers to make real financial decisions.

**Crosby** ("quality is free") — A split-sum invariant violation caught by a functional test costs minutes. The same bug discovered after a user imports 6 months of bank statements and sees their budget report is wrong costs hours of investigation and erodes trust in the entire system.

## Coverage Targets

| Subsystem | Target | Why |
| ----------- | -------- | ----- |
| Domain Entities & Value Objects | 90–95% | Money arithmetic, split-sum invariants, and account balance derivation are the foundation. A rounding error in `Money.Add()` or a missed currency check cascades into every balance, budget, and net worth calculation in the system. |
| Import Pipeline (Parsers + Fingerprinting) | 85–90% | CSV/OFX/QIF parsers handle messy real-world bank files. A missed edge case (negative amounts in credit columns, date format variations, encoding issues) silently creates wrong transactions that users won't notice until reconciliation. |
| CQRS Command Handlers | 85–90% | Commands mutate financial state. An ownership check that doesn't fire, a rollback that double-deletes, or a budget calculation that ignores splits can corrupt the user's financial picture. |
| Query Handlers (Net Worth, Portfolio) | 80–85% | Net worth and portfolio queries aggregate across accounts, currencies, and time. A query that excludes property valuations or miscalculates signed balances shows the user wrong numbers on their dashboard — the most visible failure mode. |
| Price Feed Providers | 75–80% | External API wrappers with retry/fallback. The defensive patterns (empty-list fallback, candidate symbol rotation) are the critical paths — the happy path is simple HTTP + JSON. |
| Infrastructure (DbContext, Repositories) | 75–80% | Thin EF Core layer. The critical pieces are: soft-delete query filters, xmin concurrency tokens, and timestamp management. Most repository logic is framework-provided. |

## Coverage Theater Prevention

The following test patterns are **explicitly prohibited** because they inflate coverage without catching real bugs:

1. **Asserting a function returned something without checking what.** `result.Should().NotBeNull()` on a query that returns financial data is theater. Assert the actual balance, the actual count, the actual category assignment.

2. **Testing that imports "succeeded" without verifying row content.** `result.Rows.Should().HaveCount(3)` is necessary but insufficient. Verify the parsed amounts, dates, and descriptions match the input file — that's where real parser bugs hide.

3. **Asserting mock returns what the mock was configured to return.** If you set up `_accountRepoMock.Setup(r => r.GetByIdAsync(...)).ReturnsAsync(account)` and then assert you got back `account`, you've tested Moq, not Privestio.

4. **Testing Money arithmetic with zero values only.** `Money.Zero() + Money.Zero()` always works. Test with negative amounts, mixed currencies (expecting exceptions), and real-world magnitudes (mortgage balances, micro-transactions).

5. **Split transaction tests that only test 1 split.** The split-sum invariant is trivially satisfied with 1 split. Test with 3+ splits, including negative amounts (refunds within a split), and verify the sum matches to the exact cent.

6. **Import tests with hand-crafted CSV strings that lack real-world messiness.** Real bank exports have: trailing whitespace, inconsistent date formats, quoted fields with commas, empty rows, BOM characters. Tests with sanitized 3-row CSVs miss the bugs users actually hit.

7. **Net worth tests with a single account type.** The complexity is in combining Banking (opening + transactions), Investment (holdings × prices), Property (latest valuation), and Loan (amortization) accounts. Testing with only Banking accounts misses the balance derivation logic for 80% of account types.

## Fitness-to-Purpose Scenarios

### Scenario 1: Split-Sum Invariant Violation in Multi-Category Expense

**What happens:** A user imports a $150.00 grocery transaction and splits it: $80.00 (Groceries), $45.00 (Household), $24.99 (Personal). The splits sum to $149.99 — one cent short. `Transaction.ValidateSplitInvariant()` (Transaction.cs:93-101) compares `splitTotal == Amount.Amount` using decimal equality. The validation returns `false`, but if no caller checks the return value, the transaction persists with an unbalanced split. Budget tracking for each category will be off by fractions of a cent, accumulating over months into visible discrepancies.

**Why this matters:** Financial systems must balance to the cent. A $0.01 discrepancy per split transaction × 200 transactions/month = $2.00/month drift in budget accuracy. Users who reconcile will lose trust; users who don't will make decisions on wrong numbers.

**How to verify:** Create a transaction with 3+ splits that sum to `Amount - 0.01m`. Call `ValidateSplitInvariant()`. Assert it returns `false`. Then verify that no command handler persists a transaction where `ValidateSplitInvariant()` returns false.

### Scenario 2: Cross-Currency Money Operation Without Explicit FX Conversion

**What happens:** A user has a CAD chequing account and a USD savings account. They create a transfer. If the transfer command constructs `Money("CAD").Add(Money("USD"))`, the `Money.Add()` method (Money.cs:12-18) throws `InvalidOperationException`. But if the exception is caught and swallowed by a generic handler, the transfer appears to succeed with an amount of $0 or the original CAD amount — silently wrong.

**Why this matters:** Multi-currency households are a core use case (Canadian users with USD accounts). The Money value object correctly enforces currency boundaries, but every consumer of Money must handle the exception path — not swallow it.

**How to verify:** Attempt `new Money(100m, "CAD").Add(new Money(50m, "USD"))`. Assert `InvalidOperationException` is thrown with the specific message about FX conversion. Verify `Subtract`, `>`, `<`, `>=`, `<=` operators all throw for mismatched currencies.

### Scenario 3: Duplicate Import via Identical Fingerprints

**What happens:** A user exports their January bank statement as CSV and imports it. A week later, they export the same statement again (maybe with a few new transactions appended) and import it. Without fingerprint-based deduplication, every transaction from the first import is duplicated. The user's chequing balance doubles for January. Budget reports show 2× spending.

**Why this matters:** This is the most common real-world import scenario. Users re-download statements. The import pipeline's `ImportFingerprint` on Transaction (Transaction.cs:53) must prevent duplicate creation.

**How to verify:** Import a CSV with 5 transactions. Import the same CSV again. Assert the second import creates 0 new transactions and reports all 5 as duplicates.

### Scenario 4: Property Account Balance Derived from Transactions Instead of Valuations

**What happens:** A user adds a property account with a $500,000 valuation. They then record $5,000 in property tax and $3,000 in maintenance as transactions. If the balance derivation uses `OpeningBalance + Σ Transactions` (the Banking formula), the property shows as $492,000 instead of the correct $500,000 (latest valuation). The net worth dashboard drops by $8,000 — a phantom loss.

**Why this matters:** FEATURES.md §2.4 specifies: "CurrentBalance must derive from latest Valuation by EffectiveDate, NOT from transactions. Transactions on property accounts track expenses only; must not affect estimated value." This is a different balance derivation formula per account type — the most architecturally complex part of the net worth calculation.

**How to verify:** Create a Property account with a $500,000 valuation and $10,000 in expense transactions. Query net worth. Assert the property contributes $500,000 (from valuation), not $490,000 (from opening + transactions). Also verify expenses are correctly reported in spending analysis separately.

### Scenario 5: Background Price Fetch Crashes Mid-Batch and Leaves Partial State

**What happens:** `DailyPriceFetchBackgroundService` (DailyPriceFetchBackgroundService.cs) fetches prices for 50 securities. After successfully fetching 30, the Yahoo Finance API returns a 429 (rate limited). The service catches the exception and logs it (lines 89-172). But the 30 prices already fetched are persisted while the remaining 20 have stale prices. The portfolio dashboard shows accurate values for some holdings and yesterday's prices for others — a misleading partial update.

**Why this matters:** Partial price updates create inconsistent portfolio snapshots. A user checking their portfolio at 4:01 PM sees some stocks at today's close and others at yesterday's — total portfolio value is meaningless.

**How to verify:** Mock a price feed provider that succeeds for the first N securities and throws for the rest. Run the fetch. Verify that persisted prices are consistent (either all updated or all rolled back) and that the log clearly indicates which securities failed.

### Scenario 6: Offline Sync Resurrects a Soft-Deleted Transaction

**What happens:** User A deletes a transaction on the server (soft-delete sets `IsDeleted = true`). User B, who was offline, still has the transaction in their IndexedDB. When User B comes online and syncs, if the sync logic treats the local copy as "newer" (because it was modified more recently on the client), it could un-delete the transaction — resurrecting a record the household has already removed.

**Why this matters:** FEATURES.md §9 specifies tombstone propagation to prevent resurrection. This is the canonical distributed systems bug for offline-first apps. If soft-deletes can be undone by stale clients, the "delete" operation is unreliable.

**How to verify:** Create a transaction, soft-delete it on the server (creating a SyncTombstone). Simulate a sync request from a client that still has the active transaction. Assert the response includes the tombstone and the transaction remains deleted.

### Scenario 7: Budget Tracking Uses Parent Category Instead of Split Line Categories

**What happens:** A $200 Costco transaction is split: $120 (Groceries) + $80 (Household Supplies). The parent transaction's category is "Shopping". If budget tracking sums by parent category instead of split line categories, the Groceries budget shows $0 spent and the Shopping budget shows $200 — both wrong.

**Why this matters:** FEATURES.md §4.1 and Transaction.cs:8 both state: "When IsSplit is true, budget tracking uses split line categories, not this transaction's category." This is a documented requirement that's easy to get wrong because it requires checking `IsSplit` before deciding which category to use.

**How to verify:** Create a split transaction with parent category "Shopping" and splits across "Groceries" and "Household". Query budget spend for "Groceries". Assert it includes the $120 split amount, not $0. Query "Shopping". Assert it does NOT include the $200 parent amount.

### Scenario 8: Import Rollback Leaves Orphaned Split Records

**What happens:** A user imports 50 transactions, some with splits. They realize they mapped the wrong account and roll back the import. `RollbackImportCommandHandler` soft-deletes the transactions but if `TransactionSplit` records aren't cascaded via the soft-delete, the splits remain as active records pointing to deleted parents. Budget reports that query splits directly will include these orphaned amounts.

**Why this matters:** Rollback is a critical recovery mechanism. If it doesn't cleanly undo all side effects, users lose trust in the import feature.

**How to verify:** Import transactions with splits. Roll back the import. Query for active splits belonging to the imported transactions. Assert all are soft-deleted or no longer returned by queries with the global filter.

### Scenario 9: Net Worth Calculation Excludes Loan Accounts from Liabilities

**What happens:** The net worth query handler computes `Assets - Liabilities`. If it only sums `AccountType.Banking` and `AccountType.Investment` for assets but misses `AccountType.Loan` and `AccountType.Credit` for liabilities, the user's net worth is inflated by the total of their outstanding debts. A user with $200k in assets and $180k in mortgage sees a net worth of $200k instead of $20k.

**Why this matters:** Net worth is the headline number on the dashboard. Getting it wrong by 10× destroys the entire value proposition.

**How to verify:** Create accounts of every type (Banking, Investment, Property, Credit, Loan) with known balances. Query net worth. Assert: total assets = Banking + Investment + Property, total liabilities = Credit + Loan (as absolute values), net worth = assets - liabilities.

### Scenario 10: Concurrent Import on Same Account Creates Race Condition

**What happens:** Two browser tabs both import CSV files into the same account simultaneously. Both check for existing fingerprints, find none, and proceed to create transactions. If fingerprint checking and transaction creation aren't atomic, duplicate transactions are created — the fingerprint check passed for both because neither had committed yet.

**Why this matters:** The xmin-based optimistic concurrency (PrivestioDbContext.cs:104-115) protects against concurrent updates to the same row, but it doesn't prevent two inserts from creating duplicate fingerprints across separate SaveChanges calls.

**How to verify:** This is an architectural vulnerability analysis. Verify that `ImportFingerprint` has a unique database index. If it does, the second insert will throw `DbUpdateException` — verify the error is handled gracefully rather than showing a 500 error.

## AI Session Quality Discipline

Every AI session working on Privestio must:

1. **Read QUALITY.md and AGENTS.md before writing any code.** These files define the bar.
2. **Run existing tests before and after changes.** `dotnet test` from the solution root. Zero regressions.
3. **Verify financial calculations to the cent.** Never assert approximate equality on Money values. Use exact decimal comparison.
4. **Check all five account types.** Any query or calculation that touches account balances must be tested against Banking, Credit, Investment, Property, and Loan accounts. A fix that works for Banking but breaks Property balance derivation is not a fix.
5. **Test with splits.** Any budget, category, or reporting change must be tested with split transactions. `IsSplit` changes the category resolution logic.
6. **Verify import idempotency.** Any import pipeline change must be tested by importing the same file twice. Zero duplicates on second import.
7. **No coverage theater.** Every test must assert a specific value, not just existence. See the Coverage Theater Prevention section.
8. **Use FluentAssertions consistently.** All new tests use `.Should().Be()`, `.Should().Throw<>()`, etc. Not `Assert.Equal()`.

## The Human Gate

The following decisions require human judgment — AI sessions should flag and wait:

1. **Changing balance derivation formulas.** The per-account-type balance logic (Banking vs Investment vs Property vs Loan) is a core domain decision. Propose changes; don't implement without review.
2. **Modifying sync conflict resolution policies.** Which fields are "conflict-sensitive" (Money, splits, categories) vs "last-write-wins" is a product decision.
3. **Changing import fingerprint composition.** The fingerprint formula (institution + account + date + amount + normalized memo + external reference) determines what counts as a "duplicate." Changing it affects every existing user's import history.
4. **Adding or removing account types/subtypes.** Financial taxonomy is a domain decision.
5. **Modifying the soft-delete cascade behavior.** What gets deleted when a parent is deleted (splits, tags, audit events) has compliance implications.
6. **Anything involving encryption keys, JWT configuration, or authentication providers.** Security decisions require human review.
