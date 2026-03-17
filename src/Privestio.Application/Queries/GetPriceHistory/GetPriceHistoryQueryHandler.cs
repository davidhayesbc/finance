using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPriceHistory;

public class GetPriceHistoryQueryHandler
    : IRequestHandler<GetPriceHistoryQuery, IReadOnlyList<PriceHistoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SecurityResolutionService _securityResolutionService;

    public GetPriceHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        SecurityResolutionService securityResolutionService
    )
    {
        _unitOfWork = unitOfWork;
        _securityResolutionService = securityResolutionService;
    }

    public async Task<IReadOnlyList<PriceHistoryResponse>> Handle(
        GetPriceHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        var security = await _securityResolutionService.ResolveAsync(
            request.Symbol,
            cancellationToken
        );
        if (security is null)
            return [];

        var prices = await _unitOfWork.PriceHistories.GetBySecurityIdAsync(
            security.Id,
            request.FromDate,
            request.ToDate,
            cancellationToken
        );
        return prices.Select(PriceHistoryMapper.ToResponse).ToList().AsReadOnly();
    }
}
