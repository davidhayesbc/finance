# Data Loader

Use the data loader to import manifest-driven seed data into the running API.

## Run Command

```bash
dotnet run --project tools/Privestio.DataLoader -- --manifest ~/privestio-data/manifest.json --data-dir ~/privestio-data --clear-existing-data
```

The `--clear-existing-data` flag clears existing loader-managed data for the authenticated user before import starts.

## Optional Flags

- `--api-url https://localhost:7292` to target a different API host.
- `--dry-run` to validate the run without making changes.
- `--verbose-import-errors` to print detailed row-level import diagnostics.
