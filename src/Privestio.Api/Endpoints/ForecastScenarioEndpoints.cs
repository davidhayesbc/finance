using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateForecastScenario;
using Privestio.Application.Commands.DeleteForecastScenario;
using Privestio.Application.Commands.UpdateForecastScenario;
using Privestio.Application.Queries.GetForecastScenarios;
using Privestio.Application.Queries.GetNetWorthForecast;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class ForecastScenarioEndpoints
{
    public static IEndpointRouteBuilder MapForecastScenarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/forecast-scenarios")
            .WithTags("Forecast Scenarios")
            .RequireAuthorization();

        group
            .MapGet("/", GetForecastScenariosAsync)
            .WithName("GetForecastScenarios")
            .WithSummary("Get all forecast scenarios for the current user");

        group
            .MapPost("/", CreateForecastScenarioAsync)
            .WithName("CreateForecastScenario")
            .WithSummary("Create a new forecast scenario");

        group
            .MapPut("/{id:guid}", UpdateForecastScenarioAsync)
            .WithName("UpdateForecastScenario")
            .WithSummary("Update an existing forecast scenario");

        group
            .MapDelete("/{id:guid}", DeleteForecastScenarioAsync)
            .WithName("DeleteForecastScenario")
            .WithSummary("Delete a forecast scenario");

        group
            .MapGet("/forecast/{scenarioId:guid}", GetNetWorthForecastAsync)
            .WithName("GetNetWorthForecast")
            .WithSummary("Get net worth forecast for a specific scenario");

        return app;
    }

    private static async Task<IResult> GetForecastScenariosAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetForecastScenariosQuery(userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateForecastScenarioAsync(
        [FromBody] CreateForecastScenarioRequest request,
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
            var command = new CreateForecastScenarioCommand(
                userId.Value,
                request.Name,
                request.Description,
                request.GrowthAssumptions
            );

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/forecast-scenarios/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
    }

    private static async Task<IResult> UpdateForecastScenarioAsync(
        Guid id,
        [FromBody] UpdateForecastScenarioRequest request,
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
            var command = new UpdateForecastScenarioCommand(
                id,
                userId.Value,
                request.Name,
                request.Description,
                request.GrowthAssumptions,
                false
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

    private static async Task<IResult> DeleteForecastScenarioAsync(
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
            await mediator.Send(
                new DeleteForecastScenarioCommand(id, userId.Value),
                cancellationToken
            );
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetNetWorthForecastAsync(
        Guid scenarioId,
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] int months = 60,
        CancellationToken cancellationToken = default
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        try
        {
            var result = await mediator.Send(
                new GetNetWorthForecastQuery(userId.Value, scenarioId, months),
                cancellationToken
            );
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
