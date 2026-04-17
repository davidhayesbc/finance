# Code Review Protocol: Privestio

## Bootstrap Files

Before starting any code review, read these files in order:

1. `quality/QUALITY.md` — Quality constitution (coverage targets, fitness scenarios, theater prevention)
2. `AGENTS.md` — Project context and Aspire orchestration
3. `docs/FEATURES.md` — Feature specification (sections relevant to the module under review)
4. `docs/IMPLEMENTATION-PLAN.md` — Current phase status and architectural decisions

## Focus Areas

| # | Area | Key Files | What to Check |
| --- | ------ | ----------- | --------------- |
| 1 | **Domain Invariants** | `src/Privestio.Domain/Entities/*.cs`, `src/Privestio.Domain/ValueObjects/Money.cs` | Split-sum invariant enforced at persistence, Money currency checks not bypassed, BaseEntity audit fields populated |
| 2 | **Balance Derivation** | Query handlers for accounts, net worth, portfolio | Correct formula per AccountType (Banking=Opening+Txns, Investment=Holdings×Prices, Property=LatestValuation, Loan=Opening-Principal) |
| 3 | **Import Pipeline** | `Plugins.Importers/Importers/*.cs`, `ImportTransactionsCommandHandler.cs` | Fingerprint uniqueness, per-row error collection (not fail-fast), duplicate detection before persistence, rollback completeness |
| 4 | **CQRS Handlers** | `src/Privestio.Application/Commands/*.cs`, `Queries/*.cs` | Ownership checks (UserId validation), proper cancellation token propagation, FluentValidation wired up |
| 5 | **Sync & Offline** | `ConflictResolutionService.cs`, sync endpoints, tombstone handling | Tombstone propagation, conflict-sensitive fields (Money, splits, categories) not using last-write-wins, idempotency records checked |
| 6 | **Authentication & Authorization** | `Program.cs` (JWT config), auth endpoints, middleware | JWT key validation, rate limiting on auth endpoints, ownership checks on all resource endpoints |
| 7 | **Price Feed Providers** | `Plugins.PriceSources/PriceFeeds/*.cs`, `DailyPriceFetchBackgroundService.cs` | Fallback chains work, partial fetch failures handled gracefully, ConcurrentDictionary thread safety |
| 8 | **EF Core & Data Access** | `PrivestioDbContext.cs`, repositories, migrations | Soft-delete query filters applied, xmin concurrency tokens configured, N+1 query patterns avoided |

## Mandatory Guardrails

These rules apply to every finding. Violating them produces false positives that waste developer time.

### 1. Line Numbers Are Mandatory

Every finding must include the file path and line number(s). No line number = not a real finding. Format: `src/Privestio.Domain/Entities/Transaction.cs:93-101`

### 2. Read Function Bodies, Not Just Signatures

Don't claim a function "might not handle X" without reading the implementation. Read the full method body before making any claim about its behavior.

### 3. If Unsure: Flag as QUESTION, Not BUG

If you're not certain something is wrong, label it `QUESTION` with an explanation of why you're uncertain. Only use `BUG` when you can point to specific code that contradicts a specific requirement.

### 4. Grep Before Claiming Missing

Before claiming something is missing (e.g., "no validation for X"), grep the codebase:

```bash
grep -r "X" src/ --include="*.cs"
```

If you find it, it's not missing. If you don't find it AND the spec requires it, then it's a finding.

### 5. Do NOT Suggest Style Changes

This review is for correctness, not aesthetics. Do not flag:

- Naming conventions (unless they cause confusion)
- Code formatting
- Missing XML doc comments
- Using `var` vs explicit types
- Expression-bodied members vs block bodies

**Only flag things that are incorrect, missing, or dangerous.**

## Finding Format

```markdown
### [SEVERITY] Finding Title

**File:** `path/to/file.cs:line-line`
**Category:** [Domain Invariant | Balance Logic | Import Pipeline | Auth | Data Access | Sync | Performance | Security]
**Severity:** [BUG | RISK | QUESTION]

**What I found:**
[Specific observation with line numbers]

**Why it matters:**
[Impact — what breaks, what data gets corrupted, what becomes inconsistent]

**Suggested fix:**
[Concrete code change or approach — not "add validation" but "add a check at line 47 that..."]

**Spec reference:**
[Link to FEATURES.md section, QUALITY.md scenario, or domain invariant]
```

## Severity Definitions

| Severity | Meaning | Action |
| ---------- | --------- | -------- |
| **BUG** | Code contradicts a documented requirement or produces wrong output. You can point to the spec and the code. | Must fix before merge. |
| **RISK** | Code works today but is fragile — a likely future change will break it. The failure mode is specific and plausible. | Fix in current sprint. |
| **QUESTION** | You see something unexpected but aren't sure if it's wrong. The developer may have context you don't. | Discuss — don't block. |

## Phase 2: Regression Tests

After the review produces BUG findings, write regression tests in `quality/test_regression/` that reproduce each bug.

### Regression Test Protocol

1. **For each BUG finding**, write a test that:
   - Exercises the exact code path described in the finding
   - Asserts the correct behavior per the spec
   - Fails on the current implementation (confirming the bug is real)

2. **Name tests to match findings**: `Bug_01_SplitSumNotEnforced_PersistsUnbalancedSplit`

3. **Use the project's existing test patterns**:
   - xUnit with `[Fact]` and `[Theory]`
   - FluentAssertions (`.Should().Be()`, `.Should().Throw<>()`)
   - Moq for dependencies
   - Follow the import pattern: `using Privestio.Domain.Entities;`

4. **Report results as a confirmation table:**

| Finding | Test | Result | Notes |
| --------- | ------ | -------- | ------- |
| BUG-01: Split sum not enforced | `Bug_01_SplitSumNotEnforced` | BUG CONFIRMED | Handler persists without calling ValidateSplitInvariant |
| BUG-02: Missing ownership check | `Bug_02_MissingOwnershipCheck` | FALSE POSITIVE | Check exists in endpoint middleware, not handler |
| BUG-03: Rollback orphans splits | `Bug_03_RollbackOrphansSplits` | NEEDS INVESTIGATION | EF Core cascade behavior unclear |

## Review Output Structure

Save each review to `quality/code_reviews/` with the format:
quality/code_reviews/YYYY-MM-DD_module_name.md

Include:

1. **Summary** — Module reviewed, files read, finding counts by severity
2. **Findings** — Each finding in the format above
3. **Regression test results** — Confirmation table
4. **Recommended follow-up** — What to investigate next
