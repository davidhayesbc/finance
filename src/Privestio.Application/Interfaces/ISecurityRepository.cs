using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ISecurityRepository
{
    Task<Security?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Security?> GetByAnySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default
    );
    Task<Security> AddAsync(Security security, CancellationToken cancellationToken = default);
    Task<Security> UpdateAsync(Security security, CancellationToken cancellationToken = default);
}
