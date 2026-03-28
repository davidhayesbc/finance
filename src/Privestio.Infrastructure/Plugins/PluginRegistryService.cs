using System.Reflection;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.Plugins;

internal sealed class PluginRegistryService : IPluginRegistryService
{
    private readonly IReadOnlyList<IPriceSourcePlugin> _priceSources;
    private readonly IReadOnlyList<ITransactionImporter> _transactionImporters;
    private readonly IReadOnlyList<IHoldingsImporter> _holdingsImporters;

    public PluginRegistryService(
        IEnumerable<IPriceSourcePlugin> priceSources,
        IEnumerable<ITransactionImporter> transactionImporters,
        IEnumerable<IHoldingsImporter> holdingsImporters
    )
    {
        _priceSources = priceSources.ToList();
        _transactionImporters = transactionImporters.ToList();
        _holdingsImporters = holdingsImporters.ToList();
    }

    public PluginCatalogResponse GetPluginCatalog()
    {
        var plugins = new List<RegisteredPluginResponse>();

        plugins.AddRange(_priceSources.Select(CreatePricingSourcePlugin));
        plugins.AddRange(_transactionImporters.Select(CreateTransactionImporterPlugin));
        plugins.AddRange(_holdingsImporters.Select(CreateHoldingsImporterPlugin));

        var distinct = plugins
            .GroupBy(p => p.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(p => p.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PluginCatalogResponse
        {
            Plugins = distinct,
            PricingSourceNames = _priceSources
                .Select(s => s.ProviderName.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            TransactionImportFormats = _transactionImporters
                .Select(i => i.FileFormat.Trim().ToUpperInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            HoldingsImportFormats = _holdingsImporters
                .Select(i => i.FileFormat.Trim().ToUpperInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };
    }

    public bool IsRegisteredPricingSource(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return false;

        return _priceSources.Any(p =>
            string.Equals(p.ProviderName, providerName.Trim(), StringComparison.OrdinalIgnoreCase)
        );
    }

    public bool IsRegisteredTransactionImportFormat(string fileFormat)
    {
        if (string.IsNullOrWhiteSpace(fileFormat))
            return false;

        var normalized = fileFormat.Trim().ToUpperInvariant();
        return _transactionImporters.Any(i =>
            string.Equals(i.FileFormat, normalized, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static RegisteredPluginResponse CreatePricingSourcePlugin(IPriceSourcePlugin plugin) =>
        CreatePluginDescriptor(plugin, "PricingSource", plugin.ProviderName, plugin.ProviderName);

    private static RegisteredPluginResponse CreateTransactionImporterPlugin(
        ITransactionImporter plugin
    ) =>
        CreatePluginDescriptor(plugin, "TransactionImporter", plugin.FileFormat, plugin.FileFormat);

    private static RegisteredPluginResponse CreateHoldingsImporterPlugin(
        IHoldingsImporter plugin
    ) => CreatePluginDescriptor(plugin, "HoldingsImporter", plugin.FileFormat, plugin.FileFormat);

    private static RegisteredPluginResponse CreatePluginDescriptor(
        object plugin,
        string kind,
        string name,
        string key
    )
    {
        var type = plugin.GetType();
        var assembly = type.Assembly;
        var assemblyName = assembly.GetName();
        var moduleVersionId = assembly.ManifestModule.ModuleVersionId;
        var dllIdentity = $"{assemblyName.Name}:{moduleVersionId}";

        return new RegisteredPluginResponse
        {
            Id = $"{dllIdentity}:{type.FullName}",
            Kind = kind,
            Name = name,
            Key = key,
            AssemblyName = assemblyName.Name ?? string.Empty,
            AssemblyVersion = assemblyName.Version?.ToString() ?? "0.0.0.0",
            DllIdentity = dllIdentity,
            TypeName = type.FullName ?? type.Name,
        };
    }
}
