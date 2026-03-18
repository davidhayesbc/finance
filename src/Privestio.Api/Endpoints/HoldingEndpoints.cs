using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.AddHoldingAlias;
using Privestio.Application.Commands.CreateHolding;
using Privestio.Application.Commands.DeleteHolding;
using Privestio.Application.Commands.DeleteHoldingAlias;
using Privestio.Application.Commands.UpdateHolding;
using Privestio.Application.Queries.GetHoldingAliases;
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

        group
            .MapGet("/holdings/{holdingId:guid}/aliases", GetHoldingAliasesAsync)
            .WithName("GetHoldingAliases")
            .WithSummary("Get aliases for a holding's security");

        group
            .MapPost("/holdings/{holdingId:guid}/aliases", AddHoldingAliasAsync)
            .WithName("AddHoldingAlias")
            .WithSummary("Add or update a security alias for a holding");

        group
            .MapDelete("/holdings/{holdingId:guid}/aliases/{aliasId:guid}", DeleteHoldingAliasAsync)
            .WithName("DeleteHoldingAlias")
            .WithSummary("Delete a security alias for a holding");

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

    private static async Task<IResult> GetHoldingAliasesAsync(
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
            new GetHoldingAliasesQuery(holdingId, userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> AddHoldingAliasAsync(
        Guid holdingId,
        [FromBody] AddSecurityAliasRequest request,
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
                new AddHoldingAliasCommand(
                    holdingId,
                    request.Symbol,
                    request.Source,
                    request.Exchange,
                    request.IsPrimary,
                    userId.Value
                ),
                cancellationToken
            );

            return Results.Created($"/api/v1/holdings/{holdingId}/aliases/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteHoldingAliasAsync(
        Guid holdingId,
        Guid aliasId,
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
            var deleted = await mediator.Send(
                new DeleteHoldingAliasCommand(holdingId, aliasId, userId.Value),
                cancellationToken
            );

            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
