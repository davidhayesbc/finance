# Integration Test Protocol: Privestio

## Working Directory

All commands run from the project root (`e:\src\finance\` or wherever the repo is cloned). Use relative paths only: `./src/`, `./tools/`, `./quality/`.

## Safety Constraints

1. **Never run against production data.** All tests use the Aspire-managed PostgreSQL dev container.
2. **Clear test data before each run.** Use `--clear-existing-data` flag on the data loader.
3. **External API rate limits.** Yahoo Finance and MSN Finance have undocumented rate limits. Run price feed tests sequentially, not in parallel. Space requests by 2+ seconds.
4. **Cost awareness.** Frankfurter API is free. Yahoo/MSN are free but rate-limited. No paid services are exercised.

## Pre-Flight Checks

Before running any integration test, verify:

- [ ] Aspire AppHost is running: `aspire run` (or verify via `list_resources` MCP tool)
- [ ] PostgreSQL container is healthy: check resource status for `privestio-db`
- [ ] API is responding: `curl -s https://localhost:7292/healthz` returns `Healthy`
- [ ] Web app is accessible: `curl -s https://localhost:7040` returns 200
- [ ] No pending migrations: API startup applies them automatically

## Test Matrix

### Group 1: Data Loader Pipeline (Sequential)

| # | Test | Command | Pass Criteria |
| --- | ------ | --------- | --------------- |
| 1.1 | Load seed data with manifest | `dotnet run --project tools/Privestio.DataLoader -- --manifest ~/privestio-data/manifest.json --data-dir ~/privestio-data --clear-existing-data` | Exit code 0, no error output |
| 1.2 | Dry run validation | `dotnet run --project tools/Privestio.DataLoader -- --manifest ~/privestio-data/manifest.json --data-dir ~/privestio-data --dry-run` | Exit code 0, reports what would be imported without writing |
| 1.3 | Idempotent re-load | Run 1.1 again without `--clear-existing-data` | Exit code 0, duplicate transactions reported (not created) |
| 1.4 | Verbose error diagnostics | `dotnet run --project tools/Privestio.DataLoader -- --manifest ~/privestio-data/manifest.json --data-dir ~/privestio-data --verbose-import-errors --dry-run` | Row-level diagnostics shown for any parse failures |

### Group 2: API Endpoint Smoke Tests (Parallel by domain)

Run these against the running API at `https://localhost:7292/api/v1/`:

| # | Test | Method | Endpoint | Pass Criteria |
| --- | ------ | -------- | ---------- | --------------- |
| 2.1 | List accounts | GET | `/accounts` | 200, JSON array, each has `id`, `name`, `accountType`, `currency` |
| 2.2 | Get net worth | GET | `/analytics/net-worth` | 200, JSON with `totalAssets`, `totalLiabilities`, `netWorth`, all numeric |
| 2.3 | List transactions (paginated) | GET | `/transactions?pageSize=10` | 200, returns ≤10 items with `nextCursor` if more exist |
| 2.4 | List categories (hierarchical) | GET | `/categories` | 200, JSON array with nested children |
| 2.5 | Health check | GET | `/healthz` | 200, body contains "Healthy" |
| 2.6 | Readiness check | GET | `/ready` | 200, PostgreSQL dependency check passes |
| 2.7 | Unauthorized access | GET | `/accounts` (no auth header) | 401 Unauthorized |

### Group 3: Import Pipeline End-to-End (Sequential)

| # | Test | Steps | Pass Criteria |
| --- | ------ | ------- | --------------- |
| 3.1 | CSV import happy path | Upload CSV via import endpoint → preview → commit | All rows imported, fingerprints generated, no duplicates |
| 3.2 | CSV re-import (idempotency) | Upload same CSV again | 0 new transactions, all reported as duplicates |
| 3.3 | Import rollback | Roll back import from 3.1 | All transactions soft-deleted, splits soft-deleted, import batch status = "RolledBack" |
| 3.4 | OFX import | Upload OFX/QFX file via import endpoint | Transactions created with correct dates, amounts, descriptions |
| 3.5 | CSV with errors | Upload CSV with some invalid rows | Valid rows imported, invalid rows reported with row numbers and error messages |

### Group 4: Domain Invariant Verification (Sequential)

| # | Test | Steps | Pass Criteria |
| --- | ------ | ------- | --------------- |
| 4.1 | Split-sum invariant | Create transaction with splits via API | Splits sum exactly to parent amount |
| 4.2 | Transfer linking | Create transfer between two accounts | Two linked transactions created, amounts are equal and opposite |
| 4.3 | Multi-currency rejection | Attempt to add CAD and USD money | API returns validation error, no data persisted |
| 4.4 | Soft delete cascade | Delete an account via API | Account and its transactions marked IsDeleted=true |
| 4.5 | Concurrency conflict | Update same account from two clients simultaneously | One succeeds, one gets 409 Conflict |

### Group 5: Price Feed Integration (Sequential, Rate-Limited)

| # | Test | Steps | Pass Criteria |
| --- | ------ | ------- | --------------- |
| 5.1 | Yahoo Finance lookup | Fetch price for a known symbol (e.g., AAPL) | Returns PriceQuote with valid price > 0, currency, and date |
| 5.2 | MSN Finance fallback | Configure Yahoo as primary, MSN as secondary; Yahoo fails | MSN returns valid price |
| 5.3 | Unknown symbol handling | Fetch price for "ZZZZZ.INVALID" | Empty result, no exception, warning logged |
| 5.4 | Exchange rate fetch | Fetch CAD/USD rate from Frankfurter | Returns rate > 0 with valid date |

### Group 6: E2E Browser Tests (Sequential)

| # | Test | Command | Pass Criteria |
| --- | ------ | --------- | --------------- |
| 6.1 | Playwright smoke tests | `dotnet test tests/Privestio.E2E.Tests --filter "SmokeTests"` | All tests pass |
| 6.2 | UI regression tests | `dotnet test tests/Privestio.E2E.Tests --filter "UiRegressionTests"` | All tests pass |

## Execution UX

When an AI agent runs this protocol, follow these three phases:

### Phase 1: Show the Plan (Before Running)

Display a numbered table of all test groups:

Integration Test Plan:

| # | Group | Tests | Est. Time | Dependencies |
| --- | ------- | ------- | ----------- | -------------- |
| 1 | Data Loader Pipeline | 4 | 2 min | Aspire running, manifest file |
| 2 | API Endpoint Smoke | 7 | 30 sec | Aspire running, seed data |
| 3 | Import Pipeline E2E | 5 | 3 min | Aspire running, test CSV/OFX files |
| 4 | Domain Invariants | 5 | 1 min | Aspire running, seed data |
| 5 | Price Feed Integration | 4 | 2 min | Internet access, Aspire running |
| 6 | E2E Browser Tests | 2 | 3 min | Aspire running, Playwright installed |

Ask: "Run all groups, or select specific ones?"

### Phase 2: Progress Updates (During Execution)

Report one line per test as it completes:

[1.1] ✓ Load seed data with manifest (1.2s)
[1.2] ✓ Dry run validation (0.8s)
[1.3] ✗ Idempotent re-load — expected 0 new transactions, got 3
[1.4] ✓ Verbose error diagnostics (0.6s)

### Phase 3: Summary Table (After Completion)

Integration Test Results:

| Group | Pass | Fail | Skip | Notes |
| ------- | ------ | ------ | ------ | ------- |
| Data Loader Pipeline | 3 | 1 | 0 | Idempotency check failed |
| API Endpoint Smoke | 7 | 0 | 0 | |
| Import Pipeline E2E | 5 | 0 | 0 | |
| Domain Invariants | 4 | 0 | 1 | Concurrency test needs 2 clients |
| Price Feed Integration | 3 | 0 | 1 | MSN fallback skipped (no config) |
| E2E Browser Tests | 2 | 0 | 0 | |
| **Total** | **24** | **1** | **2** | |

Recommendation: Investigate idempotency failure in Group 1 before merge.

## Post-Run Verification Checklist

After each integration test group, verify:

- [ ] No unhandled exceptions in API structured logs (`list_structured_logs` MCP tool)
- [ ] No error-level log entries that weren't expected by the test
- [ ] Database state is consistent (no orphaned records, no broken FK relationships)
- [ ] Import batches have correct status (Completed, RolledBack as expected)
- [ ] Price history records have valid `AsOfDate` and `RecordedAt` timestamps
- [ ] All account balances recalculated correctly after data changes

## Reporting Format

Save results to `quality/results/` with format:

quality/results/YYYY-MM-DD_integration_run.md

Include: test matrix results, log excerpts for failures, and follow-up recommendations.
