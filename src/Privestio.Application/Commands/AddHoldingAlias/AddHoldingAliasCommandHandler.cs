using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.AddHoldingAlias;

public class AddHoldingAliasCommandHandler
    : IRequestHandler<AddHoldingAliasCommand, SecurityAliasResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddHoldingAliasCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SecurityAliasResponse> Handle(
        AddHoldingAliasCommand request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            throw new InvalidOperationException("Holding not found.");

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            throw new InvalidOperationException("Holding not found.");

        var security = await _unitOfWork.Securities.GetByIdAsync(
            holding.SecurityId,
            cancellationToken
        );
        if (security is null)
            throw new InvalidOperationException("Security not found.");

        var alias = security.AddOrUpdateAlias(
            request.Symbol,
            request.Source,
            request.IsPrimary,
            request.Exchange
        );

        await _unitOfWork.Securities.UpdateAsync(security, cancellationToken);
        holding.RebindSecurity(security);
        await _unitOfWork.Holdings.UpdateAsync(holding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SecurityAliasMapper.ToResponse(alias);
    }
}
