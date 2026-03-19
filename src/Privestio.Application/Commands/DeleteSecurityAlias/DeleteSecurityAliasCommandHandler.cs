using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteSecurityAlias;

public class DeleteSecurityAliasCommandHandler : IRequestHandler<DeleteSecurityAliasCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSecurityAliasCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteSecurityAliasCommand request,
        CancellationToken cancellationToken
    )
    {
        var security = await _unitOfWork.Securities.GetByIdAsync(
            request.SecurityId,
            cancellationToken
        );
        if (security is null)
            return false;

        var userHasSecurity = await UserHasSecurityAsync(
            request.UserId,
            request.SecurityId,
            cancellationToken
        );
        if (!userHasSecurity)
            return false;

        var deleted = security.RemoveAlias(request.AliasId);
        if (!deleted)
            return false;

        await _unitOfWork.Securities.UpdateAsync(security, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> UserHasSecurityAsync(
        Guid userId,
        Guid securityId,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(userId, cancellationToken);
        if (accounts.Count == 0)
            return false;

        foreach (var account in accounts)
        {
            var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(
                account.Id,
                cancellationToken
            );
            if (holdings.Any(h => h.SecurityId == securityId))
                return true;
        }

        return false;
    }
}
