using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Interfaces;

public interface ISecurityRepository
{
    Task<Security?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Security?> GetByAnySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default
    );
    Task<Security?> GetByIdentifierAsync(
        SecurityIdentifierType identifierType,
        string value,
        CancellationToken cancellationToken = default
    );
    Task<Security?> GetByAliasContextAsync(
        string symbol,
        string? source,
        string? exchange,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<Security>> GetCandidatesBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default
    );
    Task<Security> AddAsync(Security security, CancellationToken cancellationToken = default);
    Task<Security> UpdateAsync(Security security, CancellationToken cancellationToken = default);
}
