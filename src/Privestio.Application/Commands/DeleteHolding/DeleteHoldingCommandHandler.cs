using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteHolding;

public class DeleteHoldingCommandHandler : IRequestHandler<DeleteHoldingCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteHoldingCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteHoldingCommand request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            return false;

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return false;

        await _unitOfWork.Holdings.DeleteAsync(request.HoldingId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
