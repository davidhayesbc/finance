using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateLot;
using Privestio.Application.Commands.DeleteLot;
using Privestio.Application.Commands.UpdateLot;
using Privestio.Application.Queries.GetLotById;
using Privestio.Application.Queries.GetLots;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class LotEndpoints
{
    public static IEndpointRouteBuilder MapLotEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Lots").RequireAuthorization();

        group
            .MapGet("/holdings/{holdingId:guid}/lots", GetLotsAsync)
            .WithName("GetLots")
            .WithSummary("Get all lots for a holding");

        group
            .MapGet("/lots/{lotId:guid}", GetLotByIdAsync)
            .WithName("GetLotById")
            .WithSummary("Get lot by id");

        group
            .MapPost("/holdings/{holdingId:guid}/lots", CreateLotAsync)
            .WithName("CreateLot")
            .WithSummary("Create a lot for a holding");

        group
            .MapPut("/lots/{lotId:guid}", UpdateLotAsync)
            .WithName("UpdateLot")
            .WithSummary("Update an existing lot");

        group
            .MapDelete("/lots/{lotId:guid}", DeleteLotAsync)
            .WithName("DeleteLot")
            .WithSummary("Delete a lot");

        return app;
    }

    private static async Task<IResult> GetLotsAsync(
        Guid holdingId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetLotsQuery(holdingId, userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetLotByIdAsync(
        Guid lotId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetLotByIdQuery(lotId, userId.Value),
            cancellationToken
        );
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateLotAsync(
        Guid holdingId,
        [FromBody] CreateLotRequest request,
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
                new CreateLotCommand(
                    holdingId,
                    request.AcquiredDate,
                    request.Quantity,
                    request.UnitCost,
                    request.Currency,
                    userId.Value,
                    request.Source,
                    request.Notes
                ),
                cancellationToken
            );

            return Results.Created($"/api/v1/lots/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateLotAsync(
        Guid lotId,
        [FromBody] UpdateLotRequest request,
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
                new UpdateLotCommand(
                    lotId,
                    request.AcquiredDate,
                    request.Quantity,
                    request.UnitCost,
                    request.Currency,
                    userId.Value,
                    request.Notes
                ),
                cancellationToken
            );

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteLotAsync(
        Guid lotId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var deleted = await mediator.Send(
            new DeleteLotCommand(lotId, userId.Value),
            cancellationToken
        );
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
