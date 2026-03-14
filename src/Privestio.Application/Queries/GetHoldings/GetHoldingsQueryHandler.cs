using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHoldings;

public class GetHoldingsQueryHandler
    : IRequestHandler<GetHoldingsQuery, IReadOnlyList<HoldingResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetHoldingsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<HoldingResponse>> Handle(
        GetHoldingsQuery request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return [];

        var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );
        return holdings.Select(HoldingMapper.ToResponse).ToList().AsReadOnly();
    }
}
