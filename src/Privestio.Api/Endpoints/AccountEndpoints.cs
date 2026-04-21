using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateAccount;
using Privestio.Application.Commands.UpdateAccount;
using Privestio.Application.Queries.GetAccountById;
using Privestio.Application.Queries.GetAccountUncategorizedCounts;
using Privestio.Application.Queries.GetAccounts;
using Privestio.Application.Queries.GetAccountValueHistory;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/accounts").WithTags("Accounts").RequireAuthorization();

        group
            .MapGet("/", GetAccountsAsync)
            .WithName("GetAccounts")
            .WithSummary("Get all accounts for the current user");

        group
            .MapGet("/{id:guid}", GetAccountByIdAsync)
            .WithName("GetAccountById")
            .WithSummary("Get a specific account by ID");

        group
            .MapGet("/uncategorized-counts", GetAccountUncategorizedCountsAsync)
            .WithName("GetAccountUncategorizedCounts")
            .WithSummary("Get uncategorized transaction counts for each accessible account");

        group
            .MapGet("/{id:guid}/history", GetAccountValueHistoryAsync)
            .WithName("GetAccountValueHistory")
            .WithSummary("Get historical balance or market value data for an account");

        group
            .MapPost("/", CreateAccountAsync)
            .WithName("CreateAccount")
            .WithSummary("Create a new account");

        group
            .MapPut("/{id:guid}", UpdateAccountAsync)
            .WithName("UpdateAccount")
            .WithSummary("Update an existing account");

        return app;
    }

    private static async Task<IResult> GetAccountsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(new GetAccountsQuery(userId.Value), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAccountByIdAsync(
        Guid id,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetAccountByIdQuery(id, userId.Value),
            cancellationToken
        );
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetAccountUncategorizedCountsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetAccountUncategorizedCountsQuery(userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAccountValueHistoryAsync(
        Guid id,
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var effectiveTo = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveFrom = fromDate ?? effectiveTo.AddYears(-1);

        if (effectiveFrom > effectiveTo)
            return Results.BadRequest(
                new { message = "fromDate must be less than or equal to toDate." }
            );

        var result = await mediator.Send(
            new GetAccountValueHistoryQuery(id, userId.Value, effectiveFrom, effectiveTo),
            cancellationToken
        );

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateAccountAsync(
        [FromBody] CreateAccountRequest request,
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
            var command = new CreateAccountCommand(
                request.Name,
                request.AccountType,
                request.AccountSubType,
                request.Currency,
                request.OpeningBalance,
                request.OpeningDate,
                userId.Value,
                request.Institution,
                request.AccountNumber,
                request.Notes
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/accounts/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
    }

    private static async Task<IResult> UpdateAccountAsync(
        Guid id,
        [FromBody] UpdateAccountRequest request,
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
            var command = new UpdateAccountCommand(
                id,
                userId.Value,
                request.Name,
                request.Institution,
                request.Notes,
                request.IsShared
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
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
