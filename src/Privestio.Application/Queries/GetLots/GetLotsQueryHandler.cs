using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetLots;

public class GetLotsQueryHandler : IRequestHandler<GetLotsQuery, IReadOnlyList<LotResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLotsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<LotResponse>> Handle(
        GetLotsQuery request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            return [];

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return [];

        var lots = await _unitOfWork.Lots.GetByHoldingIdAsync(request.HoldingId, cancellationToken);
        return lots.Select(LotMapper.ToResponse).ToList().AsReadOnly();
    }
}
