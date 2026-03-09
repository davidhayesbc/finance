using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.UpdateContributionRoom;
using Privestio.Application.Queries.GetContributionRoom;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class ContributionRoomEndpoints
{
    public static IEndpointRouteBuilder MapContributionRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contribution-room")
            .WithTags("Contribution Room")
            .RequireAuthorization();

        group
            .MapGet("/{accountId:guid}", GetContributionRoomAsync)
            .WithName("GetContributionRoom")
            .WithSummary("Get contribution room for an account");

        group
            .MapPut("/{accountId:guid}/{year:int}", UpdateContributionRoomAsync)
            .WithName("UpdateContributionRoom")
            .WithSummary("Update contribution room for an account and year");

        return app;
    }

    private static async Task<IResult> GetContributionRoomAsync(
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
                new GetContributionRoomQuery(accountId, userId.Value),
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

    private static async Task<IResult> UpdateContributionRoomAsync(
        Guid accountId,
        int year,
        [FromBody] UpdateContributionRoomRequest request,
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
            var command = new UpdateContributionRoomCommand(
                accountId,
                userId.Value,
                year,
                request.AnnualLimitAmount,
                request.CarryForwardAmount,
                request.ContributionAmount,
                request.Currency
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
}
