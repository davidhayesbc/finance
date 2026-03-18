using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHoldingSecurityIdentifiers;

public class GetHoldingSecurityIdentifiersQueryHandler
    : IRequestHandler<GetHoldingSecurityIdentifiersQuery, IReadOnlyList<SecurityIdentifierResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetHoldingSecurityIdentifiersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SecurityIdentifierResponse>> Handle(
        GetHoldingSecurityIdentifiersQuery request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            return [];

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return [];

        var security = await _unitOfWork.Securities.GetByIdAsync(holding.SecurityId, cancellationToken);
        if (security is null)
            return [];

        return security
            .Identifiers.OrderBy(i => i.IdentifierType)
            .ThenBy(i => i.Value)
            .Select(SecurityIdentifierMapper.ToResponse)
            .ToList()
            .AsReadOnly();
    }
}
