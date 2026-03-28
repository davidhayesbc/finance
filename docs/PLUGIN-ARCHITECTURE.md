# Privestio Plugin Architecture

This document describes the current plugin model for importers and pricing sources.

## Goals

- Keep plugin contracts in a dedicated contract assembly.
- Keep built-in plugin implementations in separate assemblies.
- Allow external assemblies to self-register plugin services.
- Keep Application layer dependent only on contracts, not implementation assemblies.

## Assemblies

- `src/Privestio.PluginContracts`
  - Shared contracts for plugin authors:
    - `ITransactionImporter`
    - `IHoldingsImporter`
    - `IPriceFeedProvider`
    - `IPriceSourcePlugin`
    - DTO contracts like `TransactionImportMapping`, `ImportedTransactionRow`, `PriceLookup`, `PriceQuote`
  - Plugin module contract:
    - `IPrivestioPluginModule`

- `src/Privestio.Plugins.Importers`
  - Built-in importer implementations:
    - `CsvTransactionImporter`
    - `OfxTransactionImporter`
    - `QifTransactionImporter`
    - `PdfHoldingsImporter`
  - Built-in importer module:
    - `ImportersPluginModule`

- `src/Privestio.Plugins.PriceSources`
  - Built-in price source implementations:
    - `YahooFinancePriceFeedProvider`
    - `MsnFinancePriceFeedProvider`
  - Built-in price source module:
    - `PriceSourcesPluginModule`

- `src/Privestio.Infrastructure`
  - Module bootstrap:
    - `PluginModuleLoader`
  - Composition root:
    - Loads built-in and optional external modules.
    - Composes a single `IPriceFeedProvider` via `ChainedPriceFeedProvider` over all registered `IPriceSourcePlugin` services.

## Runtime Loading

Infrastructure bootstrap uses:

- Built-in plugin assemblies (importers + price sources).
- Optional external assemblies from configuration key:
  - `Plugins:AssemblyPaths` (array of absolute or relative assembly paths).

Each plugin assembly provides one or more classes implementing `IPrivestioPluginModule`.
Each module is parameterless and receives `IServiceCollection` + `IConfiguration` in `Register(...)`.

## Writing a Custom Importer Plugin

1. Create a new class library targeting `net10.0`.
2. Reference `Privestio.PluginContracts`.
3. Implement importer contracts:
   - `ITransactionImporter` and/or `IHoldingsImporter`.
4. Add a module class implementing `IPrivestioPluginModule` and register services in `Register(...)`.
5. Build the assembly and add its path under `Plugins:AssemblyPaths` in app configuration.

Example registration:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Privestio.Domain.Interfaces;
using Privestio.PluginContracts.Hosting;

public sealed class CustomImportersModule : IPrivestioPluginModule
{
    public string ModuleName => "CustomImporters";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITransactionImporter, MyBankCsvImporter>();
    }
}
```

## Writing a Custom Price Source Plugin

1. Create a new class library targeting `net10.0`.
2. Reference `Privestio.PluginContracts`.
3. Implement `IPriceSourcePlugin`.
4. Register it from your module (`IPrivestioPluginModule`).
5. Add provider name to `Pricing:ProviderOrder` if you want it prioritized.

Example registration:

```csharp
public sealed class CustomPriceModule : IPrivestioPluginModule
{
    public string ModuleName => "CustomPriceSources";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPriceSourcePlugin, MyPriceSourcePlugin>();
    }
}
```

## Provider Selection

`ChainedPriceFeedProvider` is the application-facing `IPriceFeedProvider`.

- For latest prices: providers are tried in order until each security is resolved.
- For historical prices: providers are tried in order until one returns data.
- Provider order is read from existing `Pricing:ProviderOrder`.

## Mapping Contract Notes

`ITransactionImporter.ParseAsync(...)` now accepts `TransactionImportMapping` from `Privestio.PluginContracts`, not a domain entity. This keeps plugin contracts generic and stable for external implementers.
