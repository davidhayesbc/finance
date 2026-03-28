using Privestio.Contracts.Responses;

namespace Privestio.Application.Interfaces;

public interface IPluginRegistryService
{
    PluginCatalogResponse GetPluginCatalog();
    bool IsRegisteredPricingSource(string providerName);
    bool IsRegisteredTransactionImportFormat(string fileFormat);
}
