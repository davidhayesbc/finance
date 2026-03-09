using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateExchangeRate;
using Privestio.Application.Queries.GetExchangeRates;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class ExchangeRateEndpoints
{
    public static IEndpointRouteBuilder MapExchangeRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/exchange-rates")
            .WithTags("Exchange Rates")
            .RequireAuthorization();

        group
            .MapGet("/", GetExchangeRatesAsync)
            .WithName("GetExchangeRates")
            .WithSummary("Get exchange rates, optionally filtered by currency pair");

        group
            .MapPost("/", CreateExchangeRateAsync)
            .WithName("CreateExchangeRate")
            .WithSummary("Create a new exchange rate entry");

        return app;
    }

    private static async Task<IResult> GetExchangeRatesAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] string? fromCurrency,
        [FromQuery] string? toCurrency,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetExchangeRatesQuery(fromCurrency, toCurrency),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateExchangeRateAsync(
        [FromBody] CreateExchangeRateRequest request,
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
            var command = new CreateExchangeRateCommand(
                request.FromCurrency,
                request.ToCurrency,
                request.Rate,
                request.AsOfDate,
                request.Source,
                userId.Value
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created("/api/v1/exchange-rates", result);
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
