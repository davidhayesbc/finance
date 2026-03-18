using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateSecurityAlias;

public class UpdateSecurityAliasCommandHandler
    : IRequestHandler<UpdateSecurityAliasCommand, SecurityAliasResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSecurityAliasCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SecurityAliasResponse> Handle(
        UpdateSecurityAliasCommand request,
        CancellationToken cancellationToken
    )
    {
        var security = await _unitOfWork.Securities.GetByIdAsync(
            request.SecurityId,
            cancellationToken
        );
        if (security is null)
            throw new InvalidOperationException("Security not found.");

        var userHasSecurity = await UserHasSecurityAsync(
            request.UserId,
            request.SecurityId,
            cancellationToken
        );
        if (!userHasSecurity)
            throw new InvalidOperationException("Security not found.");

        var alias = security.UpdateAlias(
            request.AliasId,
            request.Symbol,
            request.Source,
            request.Exchange,
            request.IsPrimary
        );

        await _unitOfWork.Securities.UpdateAsync(security, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SecurityAliasMapper.ToResponse(alias);
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
