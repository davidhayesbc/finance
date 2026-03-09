using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly PrivestioDbContext _context;

    public ExchangeRateRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<ExchangeRate?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _context.ExchangeRates.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<ExchangeRate?> GetLatestByPairAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .ExchangeRates.Where(e => e.FromCurrency == fromCurrency && e.ToCurrency == toCurrency)
            .OrderByDescending(e => e.AsOfDate)
            .ThenByDescending(e => e.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ExchangeRate> AddAsync(
        ExchangeRate exchangeRate,
        CancellationToken cancellationToken = default
    )
    {
        await _context.ExchangeRates.AddAsync(exchangeRate, cancellationToken);
        return exchangeRate;
    }

    public async Task AddRangeAsync(
        IEnumerable<ExchangeRate> exchangeRates,
        CancellationToken cancellationToken = default
    )
    {
        await _context.ExchangeRates.AddRangeAsync(exchangeRates, cancellationToken);
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetAllAsync(
        string? fromCurrency = null,
        string? toCurrency = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.ExchangeRates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(fromCurrency))
            query = query.Where(e => e.FromCurrency == fromCurrency);

        if (!string.IsNullOrWhiteSpace(toCurrency))
            query = query.Where(e => e.ToCurrency == toCurrency);

        return await query
            .OrderByDescending(e => e.AsOfDate)
            .ThenByDescending(e => e.RecordedAt)
            .ToListAsync(cancellationToken);
    }
}
