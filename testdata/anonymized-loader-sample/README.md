# Anonymized Data Loader Sample

This folder contains anonymized sample data for local and CI testing of `tools/Privestio.DataLoader`.

## What Is Included

- `manifest.json` - a fixture manifest with a synthetic user and sample accounts.
- `wealthsimple/activities-chequing.csv` - anonymized Wealthsimple chequing activity sample.
- `wealthsimple/activities-tfsa.csv` - anonymized Wealthsimple TFSA activity sample.
- `tangerine/joint-account.csv` - anonymized Tangerine-style chequing activity sample.

## Anonymization Notes

- All personally identifying values were removed or replaced:
  - user email and password
  - display names and account names tied to real individuals
  - account numbers/IDs
  - memo/payee descriptions that contained names, addresses, or unique identifiers
  - property address details
- CSV schemas and mapping-compatible columns are preserved so importer behavior can still be exercised.
- Numeric values and date patterns are representative for testing import and categorization logic.

## Run Example

From repository root:

```powershell
dotnet run --project tools/Privestio.DataLoader -- --manifest testdata/anonymized-loader-sample/manifest.json --data-dir testdata/anonymized-loader-sample --clear-existing-data
```
