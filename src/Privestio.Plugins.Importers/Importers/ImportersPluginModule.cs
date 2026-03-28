using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Privestio.Domain.Interfaces;
using Privestio.PluginContracts.Hosting;

namespace Privestio.Infrastructure.Importers;

public sealed class ImportersPluginModule : IPrivestioPluginModule
{
    public string ModuleName => "BuiltInImporters";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITransactionImporter, CsvTransactionImporter>();
        services.AddScoped<ITransactionImporter, OfxTransactionImporter>();
        services.AddScoped<ITransactionImporter, QifTransactionImporter>();
        services.AddScoped<IHoldingsImporter, PdfHoldingsImporter>();
    }
}
