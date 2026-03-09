using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IFxConversionRepository
{
    Task<FxConversion?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default
    );
    Task<FxConversion> AddAsync(
        FxConversion fxConversion,
        CancellationToken cancellationToken = default
    );
}
