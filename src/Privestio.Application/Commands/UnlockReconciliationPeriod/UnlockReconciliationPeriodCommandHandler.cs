using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UnlockReconciliationPeriod;

public class UnlockReconciliationPeriodCommandHandler
    : IRequestHandler<UnlockReconciliationPeriodCommand, ReconciliationPeriodResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnlockReconciliationPeriodCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReconciliationPeriodResponse> Handle(
        UnlockReconciliationPeriodCommand request,
        CancellationToken cancellationToken
    )
    {
        var period =
            await _unitOfWork.ReconciliationPeriods.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"ReconciliationPeriod {request.Id} not found.");

        period.Unlock(request.Reason);

        await _unitOfWork.ReconciliationPeriods.UpdateAsync(period, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReconciliationPeriodMapper.ToResponse(period);
    }
}
