using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateBudget;

public class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, BudgetResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateBudgetCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BudgetResponse> Handle(
        CreateBudgetCommand request,
        CancellationToken cancellationToken
    )
    {
        var existing = await _unitOfWork.Budgets.GetByUserCategoryPeriodAsync(
            request.UserId,
            request.CategoryId,
            request.Year,
            request.Month,
            cancellationToken
        );

        if (existing is not null)
            throw new InvalidOperationException(
                $"A budget already exists for this category in {request.Year}-{request.Month:D2}."
            );

        var budget = new Budget(
            request.UserId,
            request.CategoryId,
            request.Year,
            request.Month,
            new Money(request.Amount, request.Currency),
            request.RolloverEnabled,
            request.Notes
        );

        await _unitOfWork.Budgets.AddAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BudgetMapper.ToResponse(budget);
    }
}
