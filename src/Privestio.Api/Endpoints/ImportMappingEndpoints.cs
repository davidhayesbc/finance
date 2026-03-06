using System.Security.Claims;
using MediatR;
using Privestio.Application.Commands.CreateImportMapping;
using Privestio.Application.Commands.DeleteImportMapping;
using Privestio.Application.Commands.UpdateImportMapping;
using Privestio.Application.Queries.GetImportMappings;
using Privestio.Application.Queries.PreviewFile;
using Privestio.Contracts.Requests;

namespace Privestio.Api.Endpoints;

public static class ImportMappingEndpoints
{
    public static IEndpointRouteBuilder MapImportMappingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/import-mappings")
            .WithTags("ImportMappings")
            .RequireAuthorization();

        group
            .MapGet("/", GetImportMappingsAsync)
            .WithName("GetImportMappings")
            .WithSummary("Get all saved import mappings for the current user");

        group
            .MapPost("/", CreateImportMappingAsync)
            .WithName("CreateImportMapping")
            .WithSummary("Create a new import mapping configuration");

        group
            .MapPut("/{id:guid}", UpdateImportMappingAsync)
            .WithName("UpdateImportMapping")
            .WithSummary("Update an import mapping configuration");

        group
            .MapDelete("/{id:guid}", DeleteImportMappingAsync)
            .WithName("DeleteImportMapping")
            .WithSummary("Delete an import mapping");

        group
            .MapPost("/preview", PreviewFileAsync)
            .WithName("PreviewFile")
            .WithSummary("Preview a file to detect columns and sample data")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> GetImportMappingsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetImportMappingsQuery(userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateImportMappingAsync(
        CreateImportMappingRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new CreateImportMappingCommand(
            request.Name,
            request.FileFormat,
            userId.Value,
            request.ColumnMappings,
            request.Institution,
            request.DateFormat,
            request.HasHeaderRow,
            request.AmountDebitColumn,
            request.AmountCreditColumn
        );

        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/v1/import-mappings/{result.Id}", result);
    }

    private static async Task<IResult> UpdateImportMappingAsync(
        Guid id,
        UpdateImportMappingRequest request,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var command = new UpdateImportMappingCommand(
            id,
            request.Name,
            request.ColumnMappings,
            userId.Value,
            request.DateFormat,
            request.HasHeaderRow,
            request.AmountDebitColumn,
            request.AmountCreditColumn
        );

        var result = await mediator.Send(command, cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> DeleteImportMappingAsync(
        Guid id,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new DeleteImportMappingCommand(id, userId.Value),
            cancellationToken
        );
        return result ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> PreviewFileAsync(
        IFormFile file,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new PreviewFileQuery(stream, file.FileName),
            cancellationToken
        );
        return Results.Ok(result);
    }
}
