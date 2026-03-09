using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.GenerateAmortizationSchedule;
using Privestio.Application.Queries.GetAmortizationSchedule;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class AmortizationEndpoints
{
    public static IEndpointRouteBuilder MapAmortizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/amortization")
            .WithTags("Amortization")
            .RequireAuthorization();

        group
            .MapGet("/{accountId:guid}", GetAmortizationScheduleAsync)
            .WithName("GetAmortizationSchedule")
            .WithSummary("Get amortization schedule for an account");

        group
            .MapPost("/generate", GenerateAmortizationScheduleAsync)
            .WithName("GenerateAmortizationSchedule")
            .WithSummary("Generate an amortization schedule");

        return app;
    }

    private static async Task<IResult> GetAmortizationScheduleAsync(
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
                new GetAmortizationScheduleQuery(accountId, userId.Value),
                cancellationToken
            );
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GenerateAmortizationScheduleAsync(
        [FromBody] GenerateAmortizationRequest request,
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
            var command = new GenerateAmortizationScheduleCommand(
                userId.Value,
                request.AccountId,
                request.PrincipalAmount,
                request.AnnualInterestRate,
                request.TermMonths,
                request.MonthlyPaymentAmount,
                request.StartDate,
                request.Currency
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/amortization/{request.AccountId}", result);
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
}
