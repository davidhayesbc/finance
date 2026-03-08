using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateRecurringTransaction;
using Privestio.Application.Commands.DeleteRecurringTransaction;
using Privestio.Application.Commands.GenerateRecurringTransactions;
using Privestio.Application.Commands.UpdateRecurringTransaction;
using Privestio.Application.Queries.GetRecurringTransactions;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class RecurringTransactionEndpoints
{
    public static IEndpointRouteBuilder MapRecurringTransactionEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        var group = app.MapGroup("/api/v1/recurring-transactions")
            .WithTags("Recurring Transactions")
            .RequireAuthorization();

        group
            .MapGet("/", GetRecurringTransactionsAsync)
            .WithName("GetRecurringTransactions")
            .WithSummary("Get all recurring transactions for the current user");

        group
            .MapPost("/", CreateRecurringTransactionAsync)
            .WithName("CreateRecurringTransaction")
            .WithSummary("Create a new recurring transaction pattern");

        group
            .MapPut("/{id:guid}", UpdateRecurringTransactionAsync)
            .WithName("UpdateRecurringTransaction")
            .WithSummary("Update an existing recurring transaction");

        group
            .MapDelete("/{id:guid}", DeleteRecurringTransactionAsync)
            .WithName("DeleteRecurringTransaction")
            .WithSummary("Delete a recurring transaction");

        group
            .MapPost("/generate", GenerateRecurringTransactionsAsync)
            .WithName("GenerateRecurringTransactions")
            .WithSummary(
                "Generate actual transactions from active recurring patterns up to a date"
            );

        return app;
    }

    private static async Task<IResult> GetRecurringTransactionsAsync(
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
            new GetRecurringTransactionsQuery(userId.Value, activeOnly ?? false),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateRecurringTransactionAsync(
        [FromBody] CreateRecurringTransactionRequest request,
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
            var command = new CreateRecurringTransactionCommand(
                userId.Value,
                request.AccountId,
                request.Description,
                request.Amount,
                request.TransactionType,
                request.Frequency,
                request.StartDate,
                request.EndDate,
                request.Currency,
                request.CategoryId,
                request.PayeeId,
                request.Notes
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/recurring-transactions/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
    }

    private static async Task<IResult> UpdateRecurringTransactionAsync(
        Guid id,
        [FromBody] UpdateRecurringTransactionRequest request,
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
            var command = new UpdateRecurringTransactionCommand(
                id,
                userId.Value,
                request.Description,
                request.Amount,
                request.TransactionType,
                request.Frequency,
                request.StartDate,
                request.EndDate,
                request.Currency,
                request.CategoryId,
                request.PayeeId,
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

    private static async Task<IResult> DeleteRecurringTransactionAsync(
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
            await mediator.Send(
                new DeleteRecurringTransactionCommand(id, userId.Value),
                cancellationToken
            );
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GenerateRecurringTransactionsAsync(
        [FromQuery] DateTime? upToDate,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new GenerateRecurringTransactionsCommand(
            userId.Value,
            upToDate ?? DateTime.UtcNow
        );

        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }
}
