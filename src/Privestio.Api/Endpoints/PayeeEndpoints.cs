using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreatePayee;
using Privestio.Application.Queries.GetPayees;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class PayeeEndpoints
{
    public static IEndpointRouteBuilder MapPayeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payees").WithTags("Payees").RequireAuthorization();

        group
            .MapGet("/", GetPayeesAsync)
            .WithName("GetPayees")
            .WithSummary("Get all payees for the current user");

        group
            .MapPost("/", CreatePayeeAsync)
            .WithName("CreatePayee")
            .WithSummary("Create a new payee");

        return app;
    }

    private static async Task<IResult> GetPayeesAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(new GetPayeesQuery(userId.Value), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreatePayeeAsync(
        [FromBody] CreatePayeeRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new CreatePayeeCommand(
            request.DisplayName,
            userId.Value,
            request.DefaultCategoryId
        );

        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/v1/payees/{result.Id}", result);
    }
}
