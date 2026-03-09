using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateReconciliationPeriod;

public class CreateReconciliationPeriodCommandHandler
    : IRequestHandler<CreateReconciliationPeriodCommand, ReconciliationPeriodResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateReconciliationPeriodCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReconciliationPeriodResponse> Handle(
        CreateReconciliationPeriodCommand request,
        CancellationToken cancellationToken
    )
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {request.AccountId} not found.");

        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException("Cannot reconcile another user's account.");

        var statementBalance = new Money(request.StatementBalanceAmount, request.Currency);

        var period = new ReconciliationPeriod(
            request.AccountId,
            request.StatementDate,
            statementBalance,
            request.Notes
        );

        await _unitOfWork.ReconciliationPeriods.AddAsync(period, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReconciliationPeriodMapper.ToResponse(period);
    }
}
