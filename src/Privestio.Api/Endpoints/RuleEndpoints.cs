using System.Security.Claims;
using MediatR;
using Privestio.Application.Commands.AcceptSuggestedCategorizationRule;
using Privestio.Application.Commands.CreateCategorizationRule;
using Privestio.Application.Commands.DeleteCategorizationRule;
using Privestio.Application.Commands.SuggestCategorizationRules;
using Privestio.Application.Commands.SuggestCategorizationRulesFromDb;
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

        group
            .MapPost("/suggestions/{accountId:guid}", SuggestRulesAsync)
            .WithName("SuggestRules")
            .WithSummary("Generate AI rule suggestions from an import sample")
            .DisableAntiforgery();

        group
            .MapPost("/suggestions/{accountId:guid}/from-db", SuggestRulesFromDbAsync)
            .WithName("SuggestRulesFromDb")
            .WithSummary("Generate AI rule suggestions from existing uncategorized transactions");

        group
            .MapPost("/suggestions/{accountId:guid}/accept", AcceptSuggestionAsync)
            .WithName("AcceptRuleSuggestion")
            .WithSummary(
                "Accept an AI suggestion, create a categorization rule, and apply it to account transactions"
            );

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

    private static async Task<IResult> SuggestRulesAsync(
        Guid accountId,
        IFormFile file,
        IMediator mediator,
        ClaimsPrincipal user,
        Guid? mappingId = null,
        int maxSuggestions = 8,
        CancellationToken cancellationToken = default
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        using var stream = file.OpenReadStream();
        var command = new SuggestCategorizationRulesCommand(
            stream,
            file.FileName,
            accountId,
            userId.Value,
            mappingId,
            Math.Max(1, Math.Min(maxSuggestions, 20))
        );

        try
        {
            var suggestions = await mediator.Send(command, cancellationToken);
            return Results.Ok(suggestions);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (NotSupportedException ex)
        {
            return Results.BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { Message = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return Results.Problem(
                title: "AI suggestion request timed out",
                detail: ex.Message,
                statusCode: StatusCodes.Status504GatewayTimeout
            );
        }
    }

    private static async Task<IResult> SuggestRulesFromDbAsync(
        Guid accountId,
        IMediator mediator,
        ClaimsPrincipal user,
        int maxSuggestions = 8,
        CancellationToken cancellationToken = default
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new SuggestCategorizationRulesFromDbCommand(
            accountId,
            userId.Value,
            Math.Max(1, Math.Min(maxSuggestions, 20))
        );

        try
        {
            var suggestions = await mediator.Send(command, cancellationToken);
            return Results.Ok(suggestions);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (TimeoutException ex)
        {
            return Results.Problem(
                title: "AI suggestion request timed out",
                detail: ex.Message,
                statusCode: StatusCodes.Status504GatewayTimeout
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { Message = ex.Message });
        }
    }

    private static async Task<IResult> AcceptSuggestionAsync(
        Guid accountId,
        AcceptRuleSuggestionRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new AcceptSuggestedCategorizationRuleCommand(
            accountId,
            userId.Value,
            request.Name,
            request.Priority,
            request.Conditions,
            request.CategoryId,
            request.IsEnabled,
            request.ApplyScope
        );

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
