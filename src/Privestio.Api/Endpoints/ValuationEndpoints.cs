using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateValuation;
using Privestio.Application.Queries.GetValuations;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class ValuationEndpoints
{
    public static IEndpointRouteBuilder MapValuationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/accounts/{accountId:guid}/valuations")
            .WithTags("Valuations")
            .RequireAuthorization();

        group
            .MapGet("/", GetValuationsAsync)
            .WithName("GetValuations")
            .WithSummary("Get all valuations for an account");

        group
            .MapPost("/", CreateValuationAsync)
            .WithName("CreateValuation")
            .WithSummary("Create a new valuation for an account");

        return app;
    }

    private static async Task<IResult> GetValuationsAsync(
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
            new GetValuationsQuery(accountId, userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateValuationAsync(
        Guid accountId,
        [FromBody] CreateValuationRequest request,
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
            var command = new CreateValuationCommand(
                accountId,
                request.Amount,
                request.Currency,
                request.EffectiveDate,
                request.Source,
                userId.Value,
                request.Notes
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/accounts/{accountId}/valuations/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
    }
}
