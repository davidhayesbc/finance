using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Privestio.Domain.Interfaces;
using Privestio.PluginContracts.Hosting;

namespace Privestio.Infrastructure.PriceFeeds;

public sealed class PriceSourcesPluginModule : IPrivestioPluginModule
{
    public string ModuleName => "BuiltInPriceSources";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<YahooFinancePriceFeedProvider>(client =>
        {
            client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Privestio/1.0 (self-hosted personal finance tracker)"
            );
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<MsnFinancePriceFeedProvider>(client =>
        {
            client.BaseAddress = new Uri("https://assets.msn.com/service/Finance/");
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Privestio/1.0 (self-hosted personal finance tracker)"
            );
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IPriceSourcePlugin>(sp =>
            sp.GetRequiredService<YahooFinancePriceFeedProvider>()
        );
        services.AddScoped<IPriceSourcePlugin>(sp =>
            sp.GetRequiredService<MsnFinancePriceFeedProvider>()
        );
    }
}
