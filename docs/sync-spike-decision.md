# Sync Spike Decision Document

**Date:** 2026-03-09
**Status:** GO - proceed with implementation

## Summary of Findings

During the spike investigation, we validated the core building blocks required for offline-first synchronization in the Privestio PWA:

1. **IndexedDB CRUD via JS Interop** -- We confirmed that Blazor WASM can perform full create, read, update, and delete operations against IndexedDB object stores through JavaScript interop. Latency is negligible for typical record sizes (accounts, transactions).

2. **Basic Queue/Flush is Feasible** -- A `syncQueue` object store can reliably capture mutations made while offline. Flushing the queue to the API upon reconnection works correctly for sequential replay. Batch sizes of up to 50 operations per flush cycle were tested without issues.

3. **Single Conflict Scenario Tested** -- We simulated a conflict where the same transaction was edited on two devices while one was offline. The conflict was detectable by comparing field-level timestamps, and a resolution UI was prototyped successfully.

## Recommended Approach

**Field-level conflict detection with user resolution UI.**

- Each synced entity carries a `lastModifiedUtc` timestamp per mutable field.
- On flush, the server compares incoming field timestamps against its own; if any field was modified server-side after the client's last-known value, a conflict is raised.
- The client presents a side-by-side diff allowing the user to pick "mine", "theirs", or merge per field.
- Non-conflicting fields are auto-merged without user intervention.

## Key Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Large offline queue causes slow flush | Medium | Cap queue at 200 operations; warn user if approaching limit; prioritize critical mutations (account balances) |
| Conflict resolution UI confuses users | Medium | Provide sensible defaults ("latest wins") with an "Advanced" toggle for field-level review |
| IndexedDB storage quota exceeded | Low | Monitor usage via `navigator.storage.estimate()`; proactively prompt user to sync before quota is hit |
| JS interop overhead for large datasets | Low | Batch reads/writes; use streaming deserialization for lists exceeding 500 records |
| Service worker cache and IndexedDB drift | Medium | Version both caches; clear IndexedDB stores on major schema migrations; run integrity check on app start |

## Decision

**GO -- proceed with full sync engine implementation.**

The spike demonstrated that all foundational pieces (IndexedDB CRUD, queue/flush, conflict detection) work within the Blazor WASM + JS interop architecture. The recommended field-level conflict detection approach balances correctness with user experience. Implementation will proceed in subsequent batches, starting with the offline data stores and connectivity monitoring established in this batch.
