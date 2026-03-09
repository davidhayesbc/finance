using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetValuations;

public class GetValuationsQueryHandler
    : IRequestHandler<GetValuationsQuery, IReadOnlyList<ValuationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetValuationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ValuationResponse>> Handle(
        GetValuationsQuery request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.UserId)
            return [];

        var valuations = await _unitOfWork.Valuations.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );
        return valuations.Select(ValuationMapper.ToResponse).ToList().AsReadOnly();
    }
}
