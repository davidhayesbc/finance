# Council of Three: Spec Audit Protocol — Privestio

## Purpose

Three independent AI models audit the Privestio codebase against its specifications (FEATURES.md, IMPLEMENTATION-PLAN.md, domain model invariants). Each model has different blind spots — cross-referencing catches defects that any single model misses.

## Audit Prompt

Copy-paste the following prompt into each of the three AI models. Do not modify the guardrails section.

---

### Audit Prompt (Copy-Paste)

You are auditing the Privestio personal finance application codebase against its specifications.

## Your Task

Read the code and compare it against the specification documents. Find places where the code does not match what the spec says it should do.

## Files to Read First

1. quality/QUALITY.md — Quality constitution with fitness-to-purpose scenarios
2. docs/FEATURES.md — Feature specification (the source of truth)
3. docs/IMPLEMENTATION-PLAN.md — Phase status and domain model invariants

## Guardrails (MANDATORY)

- Every finding MUST include a file path and line number. No line number = discard the finding.
- READ the function body before claiming it has a bug. Don't infer from the name.
- If you're not sure, label it QUESTION, not DEFECT.
- GREP before claiming something is missing: search the codebase before saying "X is not implemented."
- Do NOT report style issues, naming conventions, or missing comments.
- Do NOT report issues in test files unless they mask a real bug.

## Scrutiny Areas (Privestio-Specific)

Focus your audit on these 10 areas, in priority order:

1. **Split-sum invariant enforcement** — Is ValidateSplitInvariant() called before persistence in every command handler that creates or modifies splits? (FEATURES.md §3.4, §12)
2. **Balance derivation per account type** — Does the net worth query use the correct formula for each AccountType? Banking=Opening+Σtxns, Investment=Holdings×Prices, Property=LatestValuation, Loan=Opening-ΣPrincipal. (FEATURES.md §2.1-2.4)
3. **Import idempotency** — Does the import pipeline check ImportFingerprint before creating transactions? Is the fingerprint composition correct (institution+account+date+amount+memo+externalRef)? (FEATURES.md §3.1)
4. **Budget split-awareness** — Do budget calculations use split line categories when IsSplit is true? (FEATURES.md §4.1, Transaction.cs:8)
5. **Soft-delete cascade completeness** — When a parent entity is soft-deleted, are all child entities (splits, tags, audit events) also soft-deleted or filtered? (FEATURES.md §7, §12)
6. **Sync tombstone propagation** — Are soft-deletes propagated as SyncTombstones to offline clients? Can a stale client resurrect a deleted record? (FEATURES.md §9)
7. **Multi-currency safety** — Does every Money arithmetic operation enforce same-currency checks? Are there any paths that bypass Money.Add/Subtract and operate on raw decimals? (Money.cs)
8. **Ownership authorization** — Does every command handler and endpoint verify that the requesting user owns the resource being modified? (FEATURES.md §6.2)
9. **Concurrency protection** — Is xmin configured as a row version token for all entities inheriting BaseEntity? Are DbUpdateConcurrencyException handled at the API layer? (IMPLEMENTATION-PLAN.md §4)
10. **Price feed partial failure** — Does DailyPriceFetchBackgroundService handle partial failures (some securities fetched, others failed) without leaving inconsistent state? (QUALITY.md Scenario 5)

## Output Format

For each finding:

### [DEFECT|RISK|QUESTION] Finding Title

**File:** path/to/file.cs:line
**Spec reference:** FEATURES.md §X.Y / QUALITY.md Scenario N / IMPLEMENTATION-PLAN.md §X
**Confidence:** [HIGH|MEDIUM|LOW]

**What the spec says:**
[Quote or paraphrase the requirement]

**What the code does:**
[Specific observation with line numbers]

**Impact:**
[What breaks or becomes inconsistent]

---

## Model Selection

Choose three models with complementary strengths:

| Slot | Recommended Model | Strength | Blind Spot |
| ------ | ------------------- | ---------- | ------------ |
| Auditor 1 | Claude Opus | Deep code reasoning, catches subtle invariant violations | May over-report architectural concerns that are intentional |
| Auditor 2 | GPT-4o / o3 | Strong at spec-to-code traceability, catches missing features | May miss nuanced type system issues |
| Auditor 3 | Gemini 2.5 Pro | Good at pattern recognition, catches inconsistencies across files | May not deep-read complex LINQ/EF queries |

You can substitute models, but use at least two different model families to maximize blind-spot coverage.

## Triage Process

After all three auditors complete their reports:

### Step 1: Merge Findings

Create a unified findings table:

| # | Title | Auditor(s) | Confidence | Agreement |
| --- | ------- | ----------- | ------------ | ----------- |
| 1 | Split-sum not checked in UpdateTransactionHandler | A1, A2 | HIGH | 2/3 agree |
| 2 | Property balance uses transaction sum | A1 | MEDIUM | 1/3 (unique finding) |
| 3 | Missing ownership check on DELETE /categories | A2, A3 | HIGH | 2/3 agree |

### Step 2: Classify by Agreement

- **3/3 agree** — Almost certainly real. Fix immediately.
- **2/3 agree** — Likely real. Investigate and fix.
- **1/3 unique** — May be real or may be a model hallucination. Investigate before acting.
- **Contradictory** — Models disagree about what the code does. Re-read the code manually.

### Step 3: Verify Before Fixing

For every finding, before writing a fix:

1. Read the cited file and line number. Confirm the code matches what the auditor described.
2. Read the cited spec section. Confirm the requirement matches what the auditor quoted.
3. If both match, it's a confirmed finding. If either doesn't match, it's a false positive.

### Step 4: Fix Execution

Fix in small batches by subsystem:

1. **Batch by module** — All domain invariant fixes together, all import pipeline fixes together, etc.
2. **One commit per batch** — Not one mega-commit with all fixes.
3. **Run `dotnet test` after each batch** — Verify no regressions.
4. **Write regression tests for DEFECT findings** — Each fix should have a corresponding test in `quality/test_regression/`.

## Save Results

Save the merged audit report to:
quality/spec_audits/YYYY-MM-DD_council_audit.md

Include:

1. Which models were used
2. The merged findings table with agreement levels
3. Triage decisions (fix, investigate, false positive)
4. Fix execution log with test results
