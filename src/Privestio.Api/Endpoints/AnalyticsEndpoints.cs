using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Queries.GetCashFlowSummary;
using Privestio.Application.Queries.GetDebtOverview;
using Privestio.Application.Queries.GetNetWorthHistory;
using Privestio.Application.Queries.GetNetWorthSummary;
using Privestio.Application.Queries.GetSpendingAnalysis;

namespace Privestio.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/analytics").WithTags("Analytics").RequireAuthorization();

        group
            .MapGet("/net-worth", GetNetWorthSummaryAsync)
            .WithName("GetNetWorthSummary")
            .WithSummary("Get net worth summary for the current user");

        group
            .MapGet("/net-worth/history", GetNetWorthHistoryAsync)
            .WithName("GetNetWorthHistory")
            .WithSummary("Get net worth history for the current user");

        group
            .MapGet("/spending", GetSpendingAnalysisAsync)
            .WithName("GetSpendingAnalysis")
            .WithSummary("Get spending analysis for a date range");

        group
            .MapGet("/cash-flow", GetCashFlowSummaryAsync)
            .WithName("GetCashFlowSummary")
            .WithSummary("Get cash flow summary for a date range");

        group
            .MapGet("/debt", GetDebtOverviewAsync)
            .WithName("GetDebtOverview")
            .WithSummary("Get debt overview for the current user");

        return app;
    }

    private static async Task<IResult> GetNetWorthSummaryAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetNetWorthSummaryQuery(userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetNetWorthHistoryAsync(
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
        var effectiveFrom = fromDate ?? effectiveTo.AddYears(-1);

        if (effectiveFrom > effectiveTo)
            return Results.BadRequest(
                new { message = "fromDate must be less than or equal to toDate." }
            );

        var result = await mediator.Send(
            new GetNetWorthHistoryQuery(userId.Value, effectiveFrom, effectiveTo),
            cancellationToken
        );

        return Results.Ok(result);
    }

    private static async Task<IResult> GetSpendingAnalysisAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetSpendingAnalysisQuery(userId.Value, startDate, endDate),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCashFlowSummaryAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetCashFlowSummaryQuery(userId.Value, startDate, endDate),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDebtOverviewAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(new GetDebtOverviewQuery(userId.Value), cancellationToken);
        return Results.Ok(result);
    }
}
