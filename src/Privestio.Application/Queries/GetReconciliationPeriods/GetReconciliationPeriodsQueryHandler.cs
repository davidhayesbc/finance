using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetReconciliationPeriods;

public class GetReconciliationPeriodsQueryHandler
    : IRequestHandler<GetReconciliationPeriodsQuery, IReadOnlyList<ReconciliationPeriodResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetReconciliationPeriodsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ReconciliationPeriodResponse>> Handle(
        GetReconciliationPeriodsQuery request,
        CancellationToken cancellationToken
    )
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {request.AccountId} not found.");

        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot view another user's reconciliation periods."
            );

        var periods = await _unitOfWork.ReconciliationPeriods.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );
        return periods.Select(ReconciliationPeriodMapper.ToResponse).ToList().AsReadOnly();
    }
}
