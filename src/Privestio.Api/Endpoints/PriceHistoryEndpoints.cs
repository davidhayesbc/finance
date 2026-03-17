using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreatePriceHistory;
using Privestio.Application.Commands.SyncHistoricalPrices;
using Privestio.Application.Queries.GetPriceHistory;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class PriceHistoryEndpoints
{
    public static IEndpointRouteBuilder MapPriceHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/price-history")
            .WithTags("Price History")
            .RequireAuthorization();

        group
            .MapGet("/{symbol}", GetPriceHistoryAsync)
            .WithName("GetPriceHistory")
            .WithSummary("Get price history for a symbol");

        group
            .MapPost("/", CreatePriceHistoryAsync)
            .WithName("CreatePriceHistory")
            .WithSummary("Create a single price history entry");

        group
            .MapPost("/batch", BatchCreatePriceHistoryAsync)
            .WithName("BatchCreatePriceHistory")
            .WithSummary("Create multiple price history entries, skipping duplicates");

        group
            .MapPost("/sync/historical", SyncHistoricalPricesAsync)
            .WithName("SyncHistoricalPrices")
            .WithSummary(
                "Fetch and persist historical prices for the current user's investment holdings"
            );

        return app;
    }

    private static async Task<IResult> GetPriceHistoryAsync(
        string symbol,
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

        var result = await mediator.Send(
            new GetPriceHistoryQuery(symbol, fromDate, toDate),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreatePriceHistoryAsync(
        [FromBody] CreatePriceHistoryRequest request,
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
            var entry = new PriceHistoryEntry(
                request.Symbol,
                request.Price,
                request.Currency,
                request.AsOfDate,
                request.Source
            );

            var command = new CreatePriceHistoryCommand([entry]);
            var result = await mediator.Send(command, cancellationToken);
            return result.Count > 0
                ? Results.Created($"/api/v1/price-history/{request.Symbol}", result[0])
                : Results.Ok(result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
    }

    private static async Task<IResult> BatchCreatePriceHistoryAsync(
        [FromBody] BatchCreatePriceHistoryRequest request,
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
            var entries = request
                .Entries.Select(e => new PriceHistoryEntry(
                    e.Symbol,
                    e.Price,
                    e.Currency,
                    e.AsOfDate,
                    e.Source
                ))
                .ToList();

            var command = new CreatePriceHistoryCommand(entries);
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
    }

    private static async Task<IResult> SyncHistoricalPricesAsync(
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
        var effectiveFrom = fromDate ?? effectiveTo.AddYears(-10);

        if (effectiveFrom > effectiveTo)
        {
            return Results.BadRequest(
                new { message = "fromDate must be less than or equal to toDate." }
            );
        }

        var result = await mediator.Send(
            new SyncHistoricalPricesCommand(userId.Value, effectiveFrom, effectiveTo),
            cancellationToken
        );

        return Results.Ok(result);
    }
}
