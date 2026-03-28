using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Privestio.PluginContracts.Hosting;

/// <summary>
/// Registers plugin-provided services into the host application's DI container.
/// </summary>
public interface IPrivestioPluginModule
{
    string ModuleName { get; }

    void Register(IServiceCollection services, IConfiguration configuration);
}
