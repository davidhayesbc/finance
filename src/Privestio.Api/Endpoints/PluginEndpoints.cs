using Privestio.Application.Interfaces;

namespace Privestio.Api.Endpoints;

public static class PluginEndpoints
{
    public static IEndpointRouteBuilder MapPluginEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/plugins").WithTags("Plugins").RequireAuthorization();

        group
            .MapGet("/", GetPluginCatalog)
            .WithName("GetPluginCatalog")
            .WithSummary("Get registered pricing and importer plugins.");

        return app;
    }

    private static IResult GetPluginCatalog(IPluginRegistryService pluginRegistry)
    {
        var catalog = pluginRegistry.GetPluginCatalog();
        return Results.Ok(catalog);
    }
}
