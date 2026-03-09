using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.ResolveConflict;
using Privestio.Application.Queries.GetChangesSince;
using Privestio.Application.Queries.GetPendingConflicts;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sync").WithTags("Sync").RequireAuthorization();

        group
            .MapGet("/changes", GetChangesAsync)
            .WithName("GetSyncChanges")
            .WithSummary("Get changes since the last sync token for a device");

        group
            .MapGet("/conflicts", GetPendingConflictsAsync)
            .WithName("GetPendingConflicts")
            .WithSummary("Get all pending sync conflicts for the current user");

        group
            .MapPost("/conflicts/{id:guid}/resolve", ResolveConflictAsync)
            .WithName("ResolveConflict")
            .WithSummary("Resolve a sync conflict");

        return app;
    }

    private static async Task<IResult> GetChangesAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] string deviceId,
        [FromQuery] string? sinceToken,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(deviceId))
            return Results.BadRequest("deviceId is required.");

        DateTime? parsedToken = null;
        if (!string.IsNullOrWhiteSpace(sinceToken))
        {
            if (
                DateTime.TryParse(
                    sinceToken,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var parsed
                )
            )
                parsedToken = parsed;
            else
                return Results.BadRequest("Invalid sinceToken format. Use ISO 8601.");
        }

        var result = await mediator.Send(
            new GetChangesSinceQuery(userId.Value, deviceId, parsedToken),
            cancellationToken
        );

        return Results.Ok(result);
    }

    private static async Task<IResult> GetPendingConflictsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetPendingConflictsQuery(userId.Value),
            cancellationToken
        );

        return Results.Ok(result);
    }

    private static async Task<IResult> ResolveConflictAsync(
        Guid id,
        [FromBody] ResolveConflictRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        try
        {
            var result = await mediator.Send(
                new ResolveConflictCommand(
                    id,
                    userId.Value,
                    request.Resolution,
                    request.MergedData
                ),
                cancellationToken
            );

            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
