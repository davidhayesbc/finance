using System.Security.Claims;
using MediatR;
using Privestio.Application.Commands.CreateCategorizationRule;
using Privestio.Application.Commands.DeleteCategorizationRule;
using Privestio.Application.Commands.UpdateCategorizationRule;
using Privestio.Application.Queries.GetCategorizationRules;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class RuleEndpoints
{
    public static IEndpointRouteBuilder MapRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/rules")
            .WithTags("CategorizationRules")
            .RequireAuthorization();

        group
            .MapGet("/", GetRulesAsync)
            .WithName("GetRules")
            .WithSummary("Get all categorization rules for the current user");

        group
            .MapPost("/", CreateRuleAsync)
            .WithName("CreateRule")
            .WithSummary("Create a new categorization rule");

        group
            .MapPut("/{id:guid}", UpdateRuleAsync)
            .WithName("UpdateRule")
            .WithSummary("Update a categorization rule");

        group
            .MapDelete("/{id:guid}", DeleteRuleAsync)
            .WithName("DeleteRule")
            .WithSummary("Delete a categorization rule");

        return app;
    }

    private static async Task<IResult> GetRulesAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetCategorizationRulesQuery(userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateRuleAsync(
        CreateCategorizationRuleRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new CreateCategorizationRuleCommand(
            request.Name,
            request.Priority,
            request.Conditions,
            request.Actions,
            userId.Value,
            request.IsEnabled
        );

        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/v1/rules/{result.Id}", result);
    }

    private static async Task<IResult> UpdateRuleAsync(
        Guid id,
        UpdateCategorizationRuleRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new UpdateCategorizationRuleCommand(
            id,
            request.Name,
            request.Priority,
            request.Conditions,
            request.Actions,
            userId.Value,
            request.IsEnabled
        );

        var result = await mediator.Send(command, cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> DeleteRuleAsync(
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
            new DeleteCategorizationRuleCommand(id, userId.Value),
            cancellationToken
        );
        return result ? Results.NoContent() : Results.NotFound();
    }
}
