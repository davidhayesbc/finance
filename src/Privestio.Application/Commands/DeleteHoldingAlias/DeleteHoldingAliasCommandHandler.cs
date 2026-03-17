using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteHoldingAlias;

public class DeleteHoldingAliasCommandHandler : IRequestHandler<DeleteHoldingAliasCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteHoldingAliasCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteHoldingAliasCommand request,
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

        var deleted = security.RemoveAlias(request.AliasId);
        if (!deleted)
            throw new InvalidOperationException(
                "Alias could not be removed. Display aliases must be promoted first."
            );

        await _unitOfWork.Securities.UpdateAsync(security, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
