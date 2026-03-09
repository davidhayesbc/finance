using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class ForecastScenarioRepository : IForecastScenarioRepository
{
    private readonly PrivestioDbContext _context;

    public ForecastScenarioRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<ForecastScenario?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _context.ForecastScenarios.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ForecastScenario>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .ForecastScenarios.Where(s => s.UserId == userId)
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<ForecastScenario> AddAsync(
        ForecastScenario scenario,
        CancellationToken cancellationToken = default
    )
    {
        await _context.ForecastScenarios.AddAsync(scenario, cancellationToken);
        return scenario;
    }

    public async Task<ForecastScenario> UpdateAsync(
        ForecastScenario scenario,
        CancellationToken cancellationToken = default
    )
    {
        _context.ForecastScenarios.Update(scenario);
        return await Task.FromResult(scenario);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scenario = await GetByIdAsync(id, cancellationToken);
        if (scenario is not null)
        {
            scenario.SoftDelete();
        }
    }
}
