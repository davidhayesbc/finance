using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetNetWorthForecast;

public class GetNetWorthForecastQueryHandler
    : IRequestHandler<GetNetWorthForecastQuery, NetWorthForecastResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly NetWorthForecastingService _forecastingService;

    public GetNetWorthForecastQueryHandler(
        IUnitOfWork unitOfWork,
        NetWorthForecastingService forecastingService
    )
    {
        _unitOfWork = unitOfWork;
        _forecastingService = forecastingService;
    }

    public async Task<NetWorthForecastResponse> Handle(
        GetNetWorthForecastQuery request,
        CancellationToken cancellationToken
    )
    {
        return await _forecastingService.ProjectNetWorth(
            _unitOfWork,
            request.UserId,
            request.ScenarioId,
            request.Months,
            cancellationToken
        );
    }
}
