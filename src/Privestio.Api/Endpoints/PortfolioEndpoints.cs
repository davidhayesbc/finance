using System.Security.Claims;
using MediatR;
using Privestio.Application.Queries.GetPortfolioPerformance;

namespace Privestio.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/accounts").WithTags("Portfolio").RequireAuthorization();

        group
            .MapGet("/{accountId}/performance", GetPortfolioPerformanceAsync)
            .WithName("GetPortfolioPerformance")
            .WithSummary(
                "Get portfolio performance metrics (market value, gain/loss, MWR) for an investment account"
            );

        return app;
    }

    private static async Task<IResult> GetPortfolioPerformanceAsync(
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
            new GetPortfolioPerformanceQuery(accountId, userId.Value),
            cancellationToken
        );

        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
