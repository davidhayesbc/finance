using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Commands.AcceptHouseholdInvitation;
using Privestio.Application.Commands.CreateHousehold;
using Privestio.Application.Commands.InviteHouseholdMember;
using Privestio.Application.Commands.RemoveHouseholdMember;
using Privestio.Application.Commands.RenameHousehold;
using Privestio.Application.Commands.UpdateHouseholdMemberRole;
using Privestio.Application.Queries.GetHousehold;
using Privestio.Application.Queries.GetMyHousehold;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class HouseholdEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/households")
            .WithTags("Households")
            .RequireAuthorization();

        group
            .MapGet("/me", GetMyHouseholdAsync)
            .WithName("GetMyHousehold")
            .WithSummary("Get the household the current user belongs to");

        group
            .MapGet("/{householdId:guid}", GetHouseholdAsync)
            .WithName("GetHousehold")
            .WithSummary("Get a household by ID");

        group
            .MapPost("/", CreateHouseholdAsync)
            .WithName("CreateHousehold")
            .WithSummary("Create a new household");

        group
            .MapPatch("/{householdId:guid}/name", RenameHouseholdAsync)
            .WithName("RenameHousehold")
            .WithSummary("Rename a household");

        group
            .MapPost("/{householdId:guid}/invitations", InviteMemberAsync)
            .WithName("InviteHouseholdMember")
            .WithSummary("Invite a user to join the household");

        group
            .MapPost("/invitations/accept", AcceptInvitationAsync)
            .WithName("AcceptHouseholdInvitation")
            .WithSummary("Accept a household invitation by token");

        group
            .MapDelete("/{householdId:guid}/members/{userId:guid}", RemoveMemberAsync)
            .WithName("RemoveHouseholdMember")
            .WithSummary("Remove a member from the household");

        group
            .MapPut("/{householdId:guid}/members/{userId:guid}/role", UpdateMemberRoleAsync)
            .WithName("UpdateHouseholdMemberRole")
            .WithSummary("Update a member's role in the household");

        return app;
    }

    private static async Task<IResult> GetMyHouseholdAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(new GetMyHouseholdQuery(userId.Value), cancellationToken);
        return result is null ? Results.NoContent() : Results.Ok(result);
    }

    private static async Task<IResult> GetHouseholdAsync(
        Guid householdId,
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
                new GetHouseholdQuery(householdId, userId.Value),
                cancellationToken
            );
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> CreateHouseholdAsync(
        [FromBody] CreateHouseholdRequest request,
        IMediator mediator,
        IValidator<CreateHouseholdCommand> validator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new CreateHouseholdCommand(request.Name, userId.Value);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/households/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }
    }

    private static async Task<IResult> RenameHouseholdAsync(
        Guid householdId,
        [FromBody] RenameHouseholdRequest request,
        IMediator mediator,
        IValidator<RenameHouseholdCommand> validator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new RenameHouseholdCommand(householdId, request.Name, userId.Value);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

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

    private static async Task<IResult> InviteMemberAsync(
        Guid householdId,
        [FromBody] InviteHouseholdMemberRequest request,
        IMediator mediator,
        IValidator<InviteHouseholdMemberCommand> validator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new InviteHouseholdMemberCommand(
            householdId,
            request.Email,
            request.Role,
            userId.Value
        );
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Created(
                $"/api/v1/households/{householdId}/invitations/{result.Id}",
                result
            );
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

    private static async Task<IResult> AcceptInvitationAsync(
        [FromBody] AcceptHouseholdInvitationRequest request,
        IMediator mediator,
        IValidator<AcceptHouseholdInvitationCommand> validator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var email = EndpointHelpers.GetEmail(user);
        if (email is null)
            return Results.Unauthorized();

        var command = new AcceptHouseholdInvitationCommand(request.Token, userId.Value, email);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid householdId,
        Guid userId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var requestingUserId = EndpointHelpers.GetUserId(user);
        if (requestingUserId is null)
            return Results.Unauthorized();

        try
        {
            await mediator.Send(
                new RemoveHouseholdMemberCommand(householdId, userId, requestingUserId.Value),
                cancellationToken
            );
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> UpdateMemberRoleAsync(
        Guid householdId,
        Guid userId,
        [FromBody] UpdateHouseholdMemberRoleRequest request,
        IMediator mediator,
        IValidator<UpdateHouseholdMemberRoleCommand> validator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var requestingUserId = EndpointHelpers.GetUserId(user);
        if (requestingUserId is null)
            return Results.Unauthorized();

        var command = new UpdateHouseholdMemberRoleCommand(
            householdId,
            userId,
            request.Role,
            requestingUserId.Value
        );
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

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
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
