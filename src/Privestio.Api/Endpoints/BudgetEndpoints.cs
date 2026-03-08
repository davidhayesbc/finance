using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateBudget;
using Privestio.Application.Commands.DeleteBudget;
using Privestio.Application.Commands.UpdateBudget;
using Privestio.Application.Queries.GetBudgets;
using Privestio.Application.Queries.GetBudgetSummary;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/budgets").WithTags("Budgets").RequireAuthorization();

        group
            .MapGet("/", GetBudgetsAsync)
            .WithName("GetBudgets")
            .WithSummary("Get budgets for the current user, optionally filtered by year/month");

        group
            .MapGet("/summary/{year:int}/{month:int}", GetBudgetSummaryAsync)
            .WithName("GetBudgetSummary")
            .WithSummary("Get budget vs actual summary for a specific month");

        group
            .MapPost("/", CreateBudgetAsync)
            .WithName("CreateBudget")
            .WithSummary("Create a new budget allocation");

        group
            .MapPut("/{id:guid}", UpdateBudgetAsync)
            .WithName("UpdateBudget")
            .WithSummary("Update an existing budget");

        group
            .MapDelete("/{id:guid}", DeleteBudgetAsync)
            .WithName("DeleteBudget")
            .WithSummary("Delete a budget");

        return app;
    }

    private static async Task<IResult> GetBudgetsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetBudgetsQuery(userId.Value, year, month),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBudgetSummaryAsync(
        int year,
        int month,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetBudgetSummaryQuery(userId.Value, year, month),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateBudgetAsync(
        [FromBody] CreateBudgetRequest request,
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
            var command = new CreateBudgetCommand(
                userId.Value,
                request.CategoryId,
                request.Year,
                request.Month,
                request.Amount,
                request.Currency,
                request.RolloverEnabled,
                request.Notes
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/budgets/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
    }

    private static async Task<IResult> UpdateBudgetAsync(
        Guid id,
        [FromBody] UpdateBudgetRequest request,
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
            var command = new UpdateBudgetCommand(
                id,
                userId.Value,
                request.Amount,
                request.Currency,
                request.RolloverEnabled,
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

    private static async Task<IResult> DeleteBudgetAsync(
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
            await mediator.Send(new DeleteBudgetCommand(id, userId.Value), cancellationToken);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
