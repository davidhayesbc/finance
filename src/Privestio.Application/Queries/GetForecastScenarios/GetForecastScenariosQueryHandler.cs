using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetForecastScenarios;

public class GetForecastScenariosQueryHandler
    : IRequestHandler<GetForecastScenariosQuery, IReadOnlyList<ForecastScenarioResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetForecastScenariosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ForecastScenarioResponse>> Handle(
        GetForecastScenariosQuery request,
        CancellationToken cancellationToken
    )
    {
        var scenarios = await _unitOfWork.ForecastScenarios.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );
        return scenarios.Select(ForecastScenarioMapper.ToResponse).ToList().AsReadOnly();
    }
}
