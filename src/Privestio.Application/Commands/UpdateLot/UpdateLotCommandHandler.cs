using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdateLot;

public class UpdateLotCommandHandler : IRequestHandler<UpdateLotCommand, LotResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLotCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LotResponse> Handle(
        UpdateLotCommand request,
        CancellationToken cancellationToken
    )
    {
        var lot = await _unitOfWork.Lots.GetByIdAsync(request.LotId, cancellationToken);
        if (lot is null)
            throw new InvalidOperationException("Lot not found.");

        var holding = await _unitOfWork.Holdings.GetByIdAsync(lot.HoldingId, cancellationToken);
        if (holding is null)
            throw new InvalidOperationException("Lot not found.");

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Lot not found.");

        lot.Update(
            request.AcquiredDate,
            request.Quantity,
            new Money(request.UnitCost, request.Currency),
            request.Notes
        );

        await _unitOfWork.Lots.UpdateAsync(lot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LotMapper.ToResponse(lot);
    }
}
