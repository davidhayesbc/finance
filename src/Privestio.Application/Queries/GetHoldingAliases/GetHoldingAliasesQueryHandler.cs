using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHoldingAliases;

public class GetHoldingAliasesQueryHandler
    : IRequestHandler<GetHoldingAliasesQuery, IReadOnlyList<SecurityAliasResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetHoldingAliasesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SecurityAliasResponse>> Handle(
        GetHoldingAliasesQuery request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            return [];

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return [];

        var security = await _unitOfWork.Securities.GetByIdAsync(
            holding.SecurityId,
            cancellationToken
        );
        if (security is null)
            return [];

        return security
            .Aliases.OrderBy(a => a.Source)
            .ThenBy(a => a.Symbol)
            .Select(SecurityAliasMapper.ToResponse)
            .ToList()
            .AsReadOnly();
    }
}
