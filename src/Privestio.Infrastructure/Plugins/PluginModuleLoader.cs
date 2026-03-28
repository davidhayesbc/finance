using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Privestio.PluginContracts.Hosting;

namespace Privestio.Infrastructure.Plugins;

internal static class PluginModuleLoader
{
    private const string ExternalAssembliesKey = "Plugins:AssemblyPaths";

    public static void RegisterModules(
        IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] builtInAssemblies
    )
    {
        var loadedAssemblies = new List<Assembly>(builtInAssemblies);
        loadedAssemblies.AddRange(LoadExternalAssemblies(configuration));

        var moduleTypes = loadedAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                typeof(IPrivestioPluginModule).IsAssignableFrom(t)
                && t is { IsAbstract: false, IsInterface: false }
                && t.GetConstructor(Type.EmptyTypes) is not null
            )
            .DistinctBy(t => t.AssemblyQualifiedName)
            .ToList();

        foreach (var moduleType in moduleTypes)
        {
            var module = (IPrivestioPluginModule)Activator.CreateInstance(moduleType)!;
            module.Register(services, configuration);
        }
    }

    private static IReadOnlyList<Assembly> LoadExternalAssemblies(IConfiguration configuration)
    {
        var configuredPaths = configuration.GetSection(ExternalAssembliesKey).Get<string[]>() ?? [];
        var assemblies = new List<Assembly>();

        foreach (var configuredPath in configuredPaths)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                continue;

            var fullPath = Path.GetFullPath(configuredPath);
            if (!File.Exists(fullPath))
                continue;

            assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath));
        }

        return assemblies;
    }
}
