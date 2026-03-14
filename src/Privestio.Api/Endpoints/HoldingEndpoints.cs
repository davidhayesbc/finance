using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateHolding;
using Privestio.Application.Commands.DeleteHolding;
using Privestio.Application.Commands.UpdateHolding;
using Privestio.Application.Queries.GetHoldingById;
using Privestio.Application.Queries.GetHoldings;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class HoldingEndpoints
{
    public static IEndpointRouteBuilder MapHoldingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Holdings").RequireAuthorization();

        group
            .MapGet("/accounts/{accountId:guid}/holdings", GetHoldingsAsync)
            .WithName("GetHoldings")
            .WithSummary("Get all holdings for an account");

        group
            .MapGet("/holdings/{holdingId:guid}", GetHoldingByIdAsync)
            .WithName("GetHoldingById")
            .WithSummary("Get holding by id");

        group
            .MapPost("/accounts/{accountId:guid}/holdings", CreateHoldingAsync)
            .WithName("CreateHolding")
            .WithSummary("Create a holding for an account");

        group
            .MapPut("/holdings/{holdingId:guid}", UpdateHoldingAsync)
            .WithName("UpdateHolding")
            .WithSummary("Update an existing holding");

        group
            .MapDelete("/holdings/{holdingId:guid}", DeleteHoldingAsync)
            .WithName("DeleteHolding")
            .WithSummary("Delete a holding");

        return app;
    }

    private static async Task<IResult> GetHoldingsAsync(
        Guid accountId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetHoldingsQuery(accountId, userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetHoldingByIdAsync(
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
            new GetHoldingByIdQuery(holdingId, userId.Value),
            cancellationToken
        );
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateHoldingAsync(
        Guid accountId,
        [FromBody] CreateHoldingRequest request,
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
                new CreateHoldingCommand(
                    accountId,
                    request.Symbol,
                    request.SecurityName,
                    request.Quantity,
                    request.AverageCostPerUnit,
                    request.Currency,
                    userId.Value,
                    request.Notes
                ),
                cancellationToken
            );

            return Results.Created($"/api/v1/holdings/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateHoldingAsync(
        Guid holdingId,
        [FromBody] UpdateHoldingRequest request,
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
                new UpdateHoldingCommand(
                    holdingId,
                    request.SecurityName,
                    request.Quantity,
                    request.AverageCostPerUnit,
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

    private static async Task<IResult> DeleteHoldingAsync(
        Guid holdingId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var deleted = await mediator.Send(
            new DeleteHoldingCommand(holdingId, userId.Value),
            cancellationToken
        );
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
