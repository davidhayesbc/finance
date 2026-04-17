# Data Loader

Use the data loader to import manifest-driven seed data into the running API.

## Run Command

```powershell
dotnet run --project tools/Privestio.DataLoader -- --manifest $env:OneDrive/privestio-data/manifest.json --data-dir $env:OneDrive/privestio-data --clear-existing-data
```

The `--clear-existing-data` flag clears existing loader-managed data for the authenticated user before import starts.

## Repository Sample Data (Anonymized)

Use the repository fixture set when you need safe, check-in-ready test data:

```powershell
dotnet run --project tools/Privestio.DataLoader -- --manifest testdata/anonymized-loader-sample/manifest.json --data-dir testdata/anonymized-loader-sample --clear-existing-data
```

The sample dataset lives under `testdata/anonymized-loader-sample` and is anonymized for local and CI testing.

## Required Flags

- `--manifest <path>` — path to the manifest JSON file.

## Optional Flags

- `--data-dir <path>` — path to the data directory. Defaults to the parent directory of the manifest file if omitted.
- `--api-url <url>` — target a different API host (default: `https://localhost:7292`).
- `--dry-run` — validate the run without making changes.
- `--verbose-import-errors` — print detailed row-level import diagnostics.
- `--clear-existing-data` — clear existing loader-managed data for the authenticated user before import starts.

## Environment Variables

All flags can alternatively be set via environment variables. CLI flags take precedence over environment variables.

| Flag | Environment Variable |
| ------ | --------------------- |
| `--manifest` | `PRIVESTIO_MANIFEST` |
| `--data-dir` | `PRIVESTIO_DATA_DIR` |
| `--api-url` | `PRIVESTIO_API_URL` |
| `--clear-existing-data` | `PRIVESTIO_CLEAR_EXISTING_DATA` |
| `--verbose-import-errors` | `PRIVESTIO_VERBOSE_IMPORT_ERRORS` |
