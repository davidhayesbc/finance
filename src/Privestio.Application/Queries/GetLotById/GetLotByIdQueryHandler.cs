using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetLotById;

public class GetLotByIdQueryHandler : IRequestHandler<GetLotByIdQuery, LotResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLotByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LotResponse?> Handle(
        GetLotByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var lot = await _unitOfWork.Lots.GetByIdAsync(request.LotId, cancellationToken);
        if (lot is null)
            return null;

        var holding = await _unitOfWork.Holdings.GetByIdAsync(lot.HoldingId, cancellationToken);
        if (holding is null)
            return null;

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return null;

        return LotMapper.ToResponse(lot);
    }
}
