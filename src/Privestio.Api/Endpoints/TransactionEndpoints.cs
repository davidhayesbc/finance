using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Queries.GetTransactions;
using Privestio.Contracts.Requests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Privestio.Application.Interfaces;
using MediatR;
using System.Security.Claims;
using FluentValidation;

namespace Privestio.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapGet("/", GetTransactionsAsync)
            .WithName("GetTransactions")
            .WithSummary("Get transactions with cursor-based pagination");

        group.MapGet("/{id:guid}", GetTransactionByIdAsync)
            .WithName("GetTransactionById")
            .WithSummary("Get a specific transaction by ID");

        group.MapPost("/", CreateTransactionAsync)
            .WithName("CreateTransaction")
            .WithSummary("Create a new transaction");

        return app;
    }

    private static async Task<IResult> GetTransactionsAsync(
        [FromQuery] Guid accountId,
        [FromQuery] int pageSize,
        [FromQuery] string? cursor,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? categoryId,
        IMediator mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var effectivePageSize = pageSize > 0 ? Math.Min(pageSize, 100) : 20;

        var result = await mediator.Send(new GetTransactionsQuery(
            accountId,
            userId.Value,
            effectivePageSize,
            cursor,
            fromDate,
            toDate,
            categoryId), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetTransactionByIdAsync(
        Guid id,
        IUnitOfWork unitOfWork,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var transaction = await unitOfWork.Transactions.GetByIdAsync(id, cancellationToken);
        if (transaction is null) return Results.NotFound();

        // Verify account ownership
        var account = await unitOfWork.Accounts.GetByIdAsync(transaction.AccountId, cancellationToken);
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null || account is null || account.OwnerId != userId.Value)
            return Results.Forbid();

        return Results.Ok(new
        {
            transaction.Id,
            transaction.AccountId,
            transaction.Date,
            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.CurrencyCode,
            transaction.Description,
            TransactionType = transaction.Type.ToString(),
            transaction.CategoryId,
            transaction.PayeeId,
            transaction.IsReconciled,
            transaction.IsSplit,
            transaction.Notes,
            transaction.CreatedAt,
            transaction.UpdatedAt,
        });
    }

    private static async Task<IResult> CreateTransactionAsync(
        [FromBody] CreateTransactionRequest request,
        IUnitOfWork unitOfWork,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        // Verify account ownership
        var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || account.OwnerId != userId.Value)
            return Results.Forbid();

        if (!Enum.TryParse<TransactionType>(request.TransactionType, out var transactionType))
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    { "TransactionType", [$"Invalid value '{request.TransactionType}'."] }
                });

        var amount = new Money(request.Amount, request.Currency);
        var transaction = new Transaction(
            request.AccountId,
            request.Date,
            amount,
            request.Description,
            transactionType);

        transaction.CategoryId = request.CategoryId;
        transaction.PayeeId = request.PayeeId;
        transaction.Notes = request.Notes;

        await unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/transactions/{transaction.Id}", new
        {
            transaction.Id,
            transaction.AccountId,
            transaction.Date,
            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.CurrencyCode,
            transaction.Description,
            TransactionType = transaction.Type.ToString(),
            transaction.CategoryId,
            transaction.PayeeId,
            transaction.CreatedAt,
        });
    }
}
