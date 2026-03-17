using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPortfolioPerformance;

public class GetPortfolioPerformanceQueryHandler
    : IRequestHandler<GetPortfolioPerformanceQuery, PortfolioPerformanceResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InvestmentPortfolioValuationService _investmentPortfolioValuationService;

    public GetPortfolioPerformanceQueryHandler(
        IUnitOfWork unitOfWork,
        InvestmentPortfolioValuationService investmentPortfolioValuationService
    )
    {
        _unitOfWork = unitOfWork;
        _investmentPortfolioValuationService = investmentPortfolioValuationService;
    }

    public async Task<PortfolioPerformanceResponse?> Handle(
        GetPortfolioPerformanceQuery request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null || account.OwnerId != request.UserId)
            return null;

        var valuation = await _investmentPortfolioValuationService.CalculateAsync(
            account,
            cancellationToken
        );

        return new PortfolioPerformanceResponse
        {
            AccountId = request.AccountId,
            Currency = valuation.Currency,
            TotalBookValue = valuation.TotalBookValue,
            TotalMarketValue = valuation.TotalMarketValue,
            TotalGainLoss = valuation.TotalGainLoss,
            TotalGainLossPercent = valuation.TotalGainLossPercent,
            PortfolioMoneyWeightedReturn = valuation.PortfolioMoneyWeightedReturn,
            CalculatedAt = valuation.CalculatedAt,
            Holdings = valuation
                .Holdings.Select(h => new HoldingPerformanceResponse
                {
                    HoldingId = h.HoldingId,
                    Symbol = h.Symbol,
                    SecurityName = h.SecurityName,
                    Quantity = h.Quantity,
                    CurrentPrice = h.CurrentPrice,
                    Currency = h.Currency,
                    QuoteCurrency = h.QuoteCurrency,
                    IsFxConverted = h.IsFxConverted,
                    FxRateToAccountCurrency = h.FxRateToAccountCurrency,
                    MarketValue = h.MarketValue,
                    BookValue = h.BookValue,
                    GainLoss = h.GainLoss,
                    GainLossPercent = h.GainLossPercent,
                    MoneyWeightedReturn = h.MoneyWeightedReturn,
                    PriceAsOfDate = h.PriceAsOfDate,
                    IsPriceStale = h.IsPriceStale,
                    PriceSource = h.PriceSource,
                })
                .ToList()
                .AsReadOnly(),
        };
    }
}
