using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateReconciliationPeriod;
using Privestio.Application.Commands.LockReconciliationPeriod;
using Privestio.Application.Commands.UnlockReconciliationPeriod;
using Privestio.Application.Queries.GetReconciliationPeriods;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class ReconciliationEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reconciliation")
            .WithTags("Reconciliation")
            .RequireAuthorization();

        group
            .MapGet("/{accountId:guid}", GetReconciliationPeriodsAsync)
            .WithName("GetReconciliationPeriods")
            .WithSummary("Get reconciliation periods for an account");

        group
            .MapPost("/", CreateReconciliationPeriodAsync)
            .WithName("CreateReconciliationPeriod")
            .WithSummary("Create a new reconciliation period");

        group
            .MapPost("/{id:guid}/lock", LockReconciliationPeriodAsync)
            .WithName("LockReconciliationPeriod")
            .WithSummary("Lock a reconciliation period");

        group
            .MapPost("/{id:guid}/unlock", UnlockReconciliationPeriodAsync)
            .WithName("UnlockReconciliationPeriod")
            .WithSummary("Unlock a reconciliation period");

        return app;
    }

    private static async Task<IResult> GetReconciliationPeriodsAsync(
        Guid accountId,
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
                new GetReconciliationPeriodsQuery(accountId, userId.Value),
                cancellationToken
            );
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> CreateReconciliationPeriodAsync(
        [FromBody] CreateReconciliationPeriodRequest request,
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
            var command = new CreateReconciliationPeriodCommand(
                userId.Value,
                request.AccountId,
                request.StatementDate,
                request.StatementBalanceAmount,
                request.Currency,
                request.Notes
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/reconciliation/{result.Id}", result);
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

    private static async Task<IResult> LockReconciliationPeriodAsync(
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
            var result = await mediator.Send(
                new LockReconciliationPeriodCommand(id, userId.Value),
                cancellationToken
            );
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> UnlockReconciliationPeriodAsync(
        Guid id,
        [FromBody] UnlockReconciliationPeriodRequest request,
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
                new UnlockReconciliationPeriodCommand(id, userId.Value, request.Reason),
                cancellationToken
            );
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
