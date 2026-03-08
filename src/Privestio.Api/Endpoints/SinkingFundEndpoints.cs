using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.ContributeSinkingFund;
using Privestio.Application.Commands.CreateSinkingFund;
using Privestio.Application.Commands.DeleteSinkingFund;
using Privestio.Application.Commands.UpdateSinkingFund;
using Privestio.Application.Queries.GetSinkingFunds;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class SinkingFundEndpoints
{
    public static IEndpointRouteBuilder MapSinkingFundEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sinking-funds")
            .WithTags("Sinking Funds")
            .RequireAuthorization();

        group
            .MapGet("/", GetSinkingFundsAsync)
            .WithName("GetSinkingFunds")
            .WithSummary("Get all sinking funds for the current user");

        group
            .MapPost("/", CreateSinkingFundAsync)
            .WithName("CreateSinkingFund")
            .WithSummary("Create a new sinking fund");

        group
            .MapPut("/{id:guid}", UpdateSinkingFundAsync)
            .WithName("UpdateSinkingFund")
            .WithSummary("Update an existing sinking fund");

        group
            .MapPost("/{id:guid}/contribute", ContributeSinkingFundAsync)
            .WithName("ContributeSinkingFund")
            .WithSummary("Record a contribution to a sinking fund");

        group
            .MapDelete("/{id:guid}", DeleteSinkingFundAsync)
            .WithName("DeleteSinkingFund")
            .WithSummary("Delete a sinking fund");

        return app;
    }

    private static async Task<IResult> GetSinkingFundsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetSinkingFundsQuery(userId.Value, activeOnly ?? false),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateSinkingFundAsync(
        [FromBody] CreateSinkingFundRequest request,
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
            var command = new CreateSinkingFundCommand(
                userId.Value,
                request.Name,
                request.TargetAmount,
                request.DueDate,
                request.Currency,
                request.AccountId,
                request.CategoryId,
                request.Notes
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/sinking-funds/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
    }

    private static async Task<IResult> UpdateSinkingFundAsync(
        Guid id,
        [FromBody] UpdateSinkingFundRequest request,
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
            var command = new UpdateSinkingFundCommand(
                id,
                userId.Value,
                request.Name,
                request.TargetAmount,
                request.DueDate,
                request.Currency,
                request.AccountId,
                request.CategoryId,
                request.Notes
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ContributeSinkingFundAsync(
        Guid id,
        [FromBody] ContributeSinkingFundRequest request,
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
            var command = new ContributeSinkingFundCommand(
                id,
                userId.Value,
                request.Amount,
                request.Currency
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> DeleteSinkingFundAsync(
        Guid id,
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
            await mediator.Send(new DeleteSinkingFundCommand(id, userId.Value), cancellationToken);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
