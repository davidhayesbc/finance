using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPriceHistory;

public class GetPriceHistoryQueryHandler
    : IRequestHandler<GetPriceHistoryQuery, IReadOnlyList<PriceHistoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPriceHistoryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PriceHistoryResponse>> Handle(
        GetPriceHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        var prices = await _unitOfWork.PriceHistories.GetBySymbolAsync(
            request.Symbol,
            request.FromDate,
            request.ToDate,
            cancellationToken
        );
        return prices.Select(PriceHistoryMapper.ToResponse).ToList().AsReadOnly();
    }
}
