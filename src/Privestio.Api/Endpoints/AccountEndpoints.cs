using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateAccount;
using Privestio.Application.Queries.GetAccountById;
using Privestio.Application.Queries.GetAccounts;
using Privestio.Contracts.Requests;
using System.Security.Claims;

namespace Privestio.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        group.MapGet("/", GetAccountsAsync)
            .WithName("GetAccounts")
            .WithSummary("Get all accounts for the current user");

        group.MapGet("/{id:guid}", GetAccountByIdAsync)
            .WithName("GetAccountById")
            .WithSummary("Get a specific account by ID");

        group.MapPost("/", CreateAccountAsync)
            .WithName("CreateAccount")
            .WithSummary("Create a new account");

        return app;
    }

    private static async Task<IResult> GetAccountsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await mediator.Send(new GetAccountsQuery(userId.Value), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAccountByIdAsync(
        Guid id,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await mediator.Send(new GetAccountByIdQuery(id, userId.Value), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateAccountAsync(
        [FromBody] CreateAccountRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null) return Results.Unauthorized();

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
                request.Notes);

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/accounts/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}
