using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class FxConversionRepository : IFxConversionRepository
{
    private readonly PrivestioDbContext _context;

    public FxConversionRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<FxConversion?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .FxConversions.Include(f => f.ExchangeRate)
            .FirstOrDefaultAsync(f => f.TransactionId == transactionId, cancellationToken);

    public async Task<FxConversion> AddAsync(
        FxConversion fxConversion,
        CancellationToken cancellationToken = default
    )
    {
        await _context.FxConversions.AddAsync(fxConversion, cancellationToken);
        return fxConversion;
    }
}
