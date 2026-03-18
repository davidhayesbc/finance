using System.Security.Claims;
using MediatR;
using Privestio.Application.Commands.ImportHoldings;

namespace Privestio.Api.Endpoints;

public static class ImportHoldingsEndpoints
{
    public static IEndpointRouteBuilder MapImportHoldingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/import/holdings")
            .WithTags("Import")
            .RequireAuthorization();

        group
            .MapPost("/{accountId:guid}", ImportHoldingsAsync)
            .WithName("ImportHoldings")
            .WithSummary("Import holdings from a PDF statement")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> ImportHoldingsAsync(
        Guid accountId,
        IFormFile file,
        IMediator mediator,
        ClaimsPrincipal user,
        DateOnly? statementDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        using var stream = file.OpenReadStream();
        var command = new ImportHoldingsCommand(
            stream,
            file.FileName,
            accountId,
            userId.Value,
            statementDate
        );

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
}
