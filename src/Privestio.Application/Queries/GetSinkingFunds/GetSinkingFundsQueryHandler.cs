using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetSinkingFunds;

public class GetSinkingFundsQueryHandler
    : IRequestHandler<GetSinkingFundsQuery, IReadOnlyList<SinkingFundResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSinkingFundsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SinkingFundResponse>> Handle(
        GetSinkingFundsQuery request,
        CancellationToken cancellationToken
    )
    {
        var funds = request.ActiveOnly
            ? await _unitOfWork.SinkingFunds.GetActiveByUserIdAsync(
                request.UserId,
                cancellationToken
            )
            : await _unitOfWork.SinkingFunds.GetByUserIdAsync(request.UserId, cancellationToken);

        return funds.Select(SinkingFundMapper.ToResponse).ToList().AsReadOnly();
    }
}
