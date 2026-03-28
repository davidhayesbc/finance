namespace Privestio.Contracts.Responses;

public record RegisteredPluginResponse
{
    public string Id { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string AssemblyName { get; init; } = string.Empty;
    public string AssemblyVersion { get; init; } = string.Empty;
    public string DllIdentity { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
}

public record PluginCatalogResponse
{
    public IReadOnlyList<RegisteredPluginResponse> Plugins { get; init; } = [];
    public IReadOnlyList<string> PricingSourceNames { get; init; } = [];
    public IReadOnlyList<string> TransactionImportFormats { get; init; } = [];
    public IReadOnlyList<string> HoldingsImportFormats { get; init; } = [];
}
