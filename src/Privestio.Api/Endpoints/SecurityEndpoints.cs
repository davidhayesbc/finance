using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Privestio.Application.Commands.AddHoldingSecurityIdentifier;
using Privestio.Application.Commands.AddSecurityAlias;
using Privestio.Application.Commands.CorrectHoldingSecurity;
using Privestio.Application.Commands.DeleteHoldingSecurityIdentifier;
using Privestio.Application.Commands.DeleteSecurityAlias;
using Privestio.Application.Commands.FetchSecurityPrice;
using Privestio.Application.Commands.SetPricingProviderOrder;
using Privestio.Application.Commands.UpdateSecurityAlias;
using Privestio.Application.Commands.UpdateSecurityDetails;
using Privestio.Application.Queries.GetHoldingSecurityIdentifiers;
using Privestio.Application.Queries.GetSecurityConflicts;
using Privestio.Application.Queries.GetUserSecurities;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/securities")
            .WithTags("Securities")
            .RequireAuthorization();

        group
            .MapGet("/", GetSecuritiesAsync)
            .WithName("GetSecurities")
            .WithSummary("Get all securities linked to the current user");

        group
            .MapPut("/{securityId:guid}", UpdateSecurityAsync)
            .WithName("UpdateSecurity")
            .WithSummary("Update security details for a security linked to the current user");

        group
            .MapPost("/{securityId:guid}/aliases", AddSecurityAliasAsync)
            .WithName("AddSecurityAlias")
            .WithSummary("Add or update an alias for a security linked to the current user");

        group
            .MapPut("/{securityId:guid}/aliases/{aliasId:guid}", UpdateSecurityAliasAsync)
            .WithName("UpdateSecurityAlias")
            .WithSummary("Update an alias for a security linked to the current user");

        group
            .MapDelete("/{securityId:guid}/aliases/{aliasId:guid}", DeleteSecurityAliasAsync)
            .WithName("DeleteSecurityAlias")
            .WithSummary("Delete an alias for a security linked to the current user");

        group
            .MapGet("/conflicts", GetConflictsAsync)
            .WithName("GetSecurityConflicts")
            .WithSummary("Get potential ambiguous security matches for current user holdings");

        group
            .MapGet("/holdings/{holdingId:guid}/identifiers", GetHoldingIdentifiersAsync)
            .WithName("GetHoldingSecurityIdentifiers")
            .WithSummary("Get identifiers for a holding's linked security");

        group
            .MapPost("/holdings/{holdingId:guid}/identifiers", AddHoldingIdentifierAsync)
            .WithName("AddHoldingSecurityIdentifier")
            .WithSummary("Add or update a security identifier for a holding's linked security");

        group
            .MapDelete(
                "/holdings/{holdingId:guid}/identifiers/{identifierId:guid}",
                DeleteHoldingIdentifierAsync
            )
            .WithName("DeleteHoldingSecurityIdentifier")
            .WithSummary("Delete an identifier from a holding's linked security");

        group
            .MapPost("/holdings/{holdingId:guid}/correct", CorrectHoldingSecurityAsync)
            .WithName("CorrectHoldingSecurity")
            .WithSummary(
                "Correct a holding's linked security using symbol/source/exchange/identifier context"
            );

        group
            .MapPut("/{securityId:guid}/pricing-order", SetPricingProviderOrderAsync)
            .WithName("SetPricingProviderOrder")
            .WithSummary("Set the pricing provider order for a specific security");

        group
            .MapPost("/{securityId:guid}/fetch-price", FetchSecurityPriceAsync)
            .WithName("FetchSecurityPrice")
            .WithSummary(
                "Fetch the latest price for a specific security from configured providers"
            );

        return app;
    }

    private static async Task<IResult> AddSecurityAliasAsync(
        Guid securityId,
        [FromBody] AddSecurityAliasRequest request,
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
                new AddSecurityAliasCommand(
                    securityId,
                    request.Symbol,
                    request.Source,
                    request.Exchange,
                    request.IsPrimary,
                    userId.Value
                ),
                cancellationToken
            );

            return Results.Created($"/api/v1/securities/{securityId}/aliases/{result.Id}", result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(
                new
                {
                    error = "The security was modified by another operation. Please refresh and try again.",
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteSecurityAliasAsync(
        Guid securityId,
        Guid aliasId,
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
            var deleted = await mediator.Send(
                new DeleteSecurityAliasCommand(securityId, aliasId, userId.Value),
                cancellationToken
            );

            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(
                new
                {
                    error = "The security was modified by another operation. Please refresh and try again.",
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateSecurityAliasAsync(
        Guid securityId,
        Guid aliasId,
        [FromBody] AddSecurityAliasRequest request,
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
                new UpdateSecurityAliasCommand(
                    securityId,
                    aliasId,
                    request.Symbol,
                    request.Source,
                    request.Exchange,
                    request.IsPrimary,
                    userId.Value
                ),
                cancellationToken
            );

            return Results.Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(
                new
                {
                    error = "The security was modified by another operation. Please refresh and try again.",
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetSecuritiesAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetUserSecuritiesQuery(userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateSecurityAsync(
        Guid securityId,
        [FromBody] UpdateSecurityDetailsRequest request,
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
                new UpdateSecurityDetailsCommand(
                    securityId,
                    request.Name,
                    request.DisplaySymbol,
                    request.Currency,
                    request.Exchange,
                    request.IsCashEquivalent,
                    userId.Value
                ),
                cancellationToken
            );

            return Results.Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(
                new
                {
                    error = "The security was modified by another operation. Please close and reopen the editor to refresh.",
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetConflictsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetSecurityConflictsQuery(userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetHoldingIdentifiersAsync(
        Guid holdingId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetHoldingSecurityIdentifiersQuery(holdingId, userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> AddHoldingIdentifierAsync(
        Guid holdingId,
        [FromBody] AddSecurityIdentifierRequest request,
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
                new AddHoldingSecurityIdentifierCommand(
                    holdingId,
                    request.IdentifierType,
                    request.Value,
                    request.IsPrimary,
                    userId.Value
                ),
                cancellationToken
            );

            return Results.Created(
                $"/api/v1/securities/holdings/{holdingId}/identifiers/{result.Id}",
                result
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteHoldingIdentifierAsync(
        Guid holdingId,
        Guid identifierId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var deleted = await mediator.Send(
            new DeleteHoldingSecurityIdentifierCommand(holdingId, identifierId, userId.Value),
            cancellationToken
        );

        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> CorrectHoldingSecurityAsync(
        Guid holdingId,
        [FromBody] CorrectHoldingSecurityRequest request,
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
                new CorrectHoldingSecurityCommand(
                    holdingId,
                    request.Symbol,
                    request.SecurityName,
                    request.Source,
                    request.Exchange,
                    request.Cusip,
                    request.Isin,
                    userId.Value
                ),
                cancellationToken
            );

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> SetPricingProviderOrderAsync(
        Guid securityId,
        [FromBody] SetPricingProviderOrderRequest request,
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
                new SetPricingProviderOrderCommand(securityId, request.ProviderOrder, userId.Value),
                cancellationToken
            );

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> FetchSecurityPriceAsync(
        Guid securityId,
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
                new FetchSecurityPriceCommand(securityId, userId.Value),
                cancellationToken
            );

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
