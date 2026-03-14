using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteLot;

public class DeleteLotCommandHandler : IRequestHandler<DeleteLotCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLotCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteLotCommand request, CancellationToken cancellationToken)
    {
        var lot = await _unitOfWork.Lots.GetByIdAsync(request.LotId, cancellationToken);
        if (lot is null)
            return false;

        var holding = await _unitOfWork.Holdings.GetByIdAsync(lot.HoldingId, cancellationToken);
        if (holding is null)
            return false;

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return false;

        await _unitOfWork.Lots.DeleteAsync(request.LotId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
