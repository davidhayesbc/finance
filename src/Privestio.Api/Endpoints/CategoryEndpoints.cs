using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateCategory;
using Privestio.Application.Queries.GetCategories;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        group
            .MapGet("/", GetCategoriesAsync)
            .WithName("GetCategories")
            .WithSummary("Get all categories for the current user");

        group
            .MapPost("/", CreateCategoryAsync)
            .WithName("CreateCategory")
            .WithSummary("Create a new category");

        return app;
    }

    private static async Task<IResult> GetCategoriesAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(new GetCategoriesQuery(userId.Value), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateCategoryAsync(
        [FromBody] CreateCategoryRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new CreateCategoryCommand(
            request.Name,
            request.Type,
            userId.Value,
            request.Icon,
            request.SortOrder,
            request.ParentCategoryId
        );

        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/v1/categories/{result.Id}", result);
    }
}
