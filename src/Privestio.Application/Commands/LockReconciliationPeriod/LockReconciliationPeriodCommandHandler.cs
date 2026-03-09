using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.LockReconciliationPeriod;

public class LockReconciliationPeriodCommandHandler
    : IRequestHandler<LockReconciliationPeriodCommand, ReconciliationPeriodResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public LockReconciliationPeriodCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReconciliationPeriodResponse> Handle(
        LockReconciliationPeriodCommand request,
        CancellationToken cancellationToken
    )
    {
        var period =
            await _unitOfWork.ReconciliationPeriods.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"ReconciliationPeriod {request.Id} not found.");

        period.Lock(request.UserId);

        await _unitOfWork.ReconciliationPeriods.UpdateAsync(period, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ReconciliationPeriodMapper.ToResponse(period);
    }
}
