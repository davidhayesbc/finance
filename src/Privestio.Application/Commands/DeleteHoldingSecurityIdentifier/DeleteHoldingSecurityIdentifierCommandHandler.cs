using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteHoldingSecurityIdentifier;

public class DeleteHoldingSecurityIdentifierCommandHandler
    : IRequestHandler<DeleteHoldingSecurityIdentifierCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteHoldingSecurityIdentifierCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteHoldingSecurityIdentifierCommand request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            return false;

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return false;

        var security = await _unitOfWork.Securities.GetByIdAsync(
            holding.SecurityId,
            cancellationToken
        );
        if (security is null)
            return false;

        var removed = security.RemoveIdentifier(request.IdentifierId);
        if (!removed)
            return false;

        await _unitOfWork.Securities.UpdateAsync(security, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
