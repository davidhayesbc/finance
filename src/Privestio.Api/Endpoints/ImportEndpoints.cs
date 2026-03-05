using System.Security.Claims;
using MediatR;
using Privestio.Application.Commands.ImportTransactions;
using Privestio.Application.Commands.RollbackImport;
using Privestio.Application.Queries.GetImportBatch;
using Privestio.Application.Queries.GetImportBatches;
using Privestio.Domain.Enums;

namespace Privestio.Api.Endpoints;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/import").WithTags("Import").RequireAuthorization();

        group
            .MapPost("/{accountId:guid}", ImportTransactionsAsync)
            .WithName("ImportTransactions")
            .WithSummary("Import transactions from a file")
            .DisableAntiforgery();

        group
            .MapPost("/{batchId:guid}/rollback", RollbackImportAsync)
            .WithName("RollbackImport")
            .WithSummary("Rollback all transactions from an import batch");

        group
            .MapGet("/batches", GetImportBatchesAsync)
            .WithName("GetImportBatches")
            .WithSummary("Get import batch history with quality metrics");

        group
            .MapGet("/batches/{batchId:guid}", GetImportBatchAsync)
            .WithName("GetImportBatch")
            .WithSummary("Get import batch details with diagnostics and error report");

        return app;
    }

    private static async Task<IResult> ImportTransactionsAsync(
        Guid accountId,
        IFormFile file,
        IMediator mediator,
        ClaimsPrincipal user,
        Guid? mappingId = null,
        ImportPolicy? policy = null,
        CancellationToken cancellationToken = default
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        using var stream = file.OpenReadStream();
        var command = new ImportTransactionsCommand(
            stream,
            file.FileName,
            accountId,
            userId.Value,
            mappingId,
            policy ?? ImportPolicy.SkipInvalid
        );

        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> RollbackImportAsync(
        Guid batchId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new RollbackImportCommand(batchId, userId.Value),
            cancellationToken
        );

        return result ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetImportBatchesAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetImportBatchesQuery(userId.Value),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetImportBatchAsync(
        Guid batchId,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetImportBatchQuery(batchId), cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}
