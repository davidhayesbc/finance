using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;

namespace Privestio.Api.Endpoints;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops").WithTags("Operations");

        group
            .MapPost("/export", ExportUserDataAsync)
            .RequireAuthorization()
            .WithName("ExportUserData")
            .WithSummary("Export all user data as a JSON file download");

        group
            .MapGet("/health/detailed", GetDetailedHealthAsync)
            .WithName("DetailedHealth")
            .WithSummary(
                "Returns detailed health information including database and service status"
            );

        return app;
    }

    private static async Task<IResult> ExportUserDataAsync(
        IDataExportService exportService,
        IUnitOfWork unitOfWork,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var json = await exportService.ExportUserDataAsync(
            unitOfWork,
            userId.Value,
            cancellationToken
        );
        var bytes = Encoding.UTF8.GetBytes(json);

        return Results.File(
            bytes,
            contentType: "application/json",
            fileDownloadName: $"privestio-export-{DateTime.UtcNow:yyyyMMddHHmmss}.json"
        );
    }

    private static async Task<IResult> GetDetailedHealthAsync(
        HealthCheckService healthCheckService,
        CancellationToken cancellationToken
    )
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);

        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
            }),
        };

        return report.Status == HealthStatus.Healthy
            ? Results.Ok(result)
            : Results.Json(result, statusCode: 503);
    }
}
