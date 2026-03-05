using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.CreateTag;
using Privestio.Application.Queries.GetTags;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tags").WithTags("Tags").RequireAuthorization();

        group
            .MapGet("/", GetTagsAsync)
            .WithName("GetTags")
            .WithSummary("Get all tags for the current user");

        group.MapPost("/", CreateTagAsync).WithName("CreateTag").WithSummary("Create a new tag");

        return app;
    }

    private static async Task<IResult> GetTagsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(new GetTagsQuery(userId.Value), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateTagAsync(
        [FromBody] CreateTagRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new CreateTagCommand(request.Name, userId.Value),
            cancellationToken
        );
        return Results.Created($"/api/v1/tags/{result.Id}", result);
    }
}
