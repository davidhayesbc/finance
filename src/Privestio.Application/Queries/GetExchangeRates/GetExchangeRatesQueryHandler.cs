using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetExchangeRates;

public class GetExchangeRatesQueryHandler
    : IRequestHandler<GetExchangeRatesQuery, IReadOnlyList<ExchangeRateResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetExchangeRatesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ExchangeRateResponse>> Handle(
        GetExchangeRatesQuery request,
        CancellationToken cancellationToken
    )
    {
        var rates = await _unitOfWork.ExchangeRates.GetAllAsync(
            request.FromCurrency,
            request.ToCurrency,
            cancellationToken
        );
        return rates.Select(ExchangeRateMapper.ToResponse).ToList().AsReadOnly();
    }
}
