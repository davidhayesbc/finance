using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IForecastScenarioRepository
{
    Task<ForecastScenario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForecastScenario>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<ForecastScenario> AddAsync(
        ForecastScenario scenario,
        CancellationToken cancellationToken = default
    );
    Task<ForecastScenario> UpdateAsync(
        ForecastScenario scenario,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
