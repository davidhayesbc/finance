using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteBudget;

public class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBudgetCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget =
            await _unitOfWork.Budgets.GetByIdAsync(request.BudgetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Budget {request.BudgetId} not found.");

        if (budget.UserId != request.UserId)
            throw new UnauthorizedAccessException("Cannot delete another user's budget.");

        await _unitOfWork.Budgets.DeleteAsync(request.BudgetId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
