using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdateBudget;

public class UpdateBudgetCommandHandler : IRequestHandler<UpdateBudgetCommand, BudgetResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBudgetCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BudgetResponse> Handle(
        UpdateBudgetCommand request,
        CancellationToken cancellationToken
    )
    {
        var budget =
            await _unitOfWork.Budgets.GetByIdAsync(request.BudgetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Budget {request.BudgetId} not found.");

        if (budget.UserId != request.UserId)
            throw new UnauthorizedAccessException("Cannot update another user's budget.");

        budget.UpdateAmount(new Money(request.Amount, request.Currency));
        budget.SetRollover(request.RolloverEnabled);
        budget.UpdateNotes(request.Notes);

        await _unitOfWork.Budgets.UpdateAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BudgetMapper.ToResponse(budget);
    }
}
