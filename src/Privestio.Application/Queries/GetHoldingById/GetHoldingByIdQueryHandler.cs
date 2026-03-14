using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHoldingById;

public class GetHoldingByIdQueryHandler : IRequestHandler<GetHoldingByIdQuery, HoldingResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetHoldingByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HoldingResponse?> Handle(
        GetHoldingByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var holding = await _unitOfWork.Holdings.GetByIdAsync(request.HoldingId, cancellationToken);
        if (holding is null)
            return null;

        var account = await _unitOfWork.Accounts.GetByIdAsync(holding.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return null;

        return HoldingMapper.ToResponse(holding);
    }
}
