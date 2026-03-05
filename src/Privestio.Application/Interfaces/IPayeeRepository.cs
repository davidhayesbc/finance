using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IPayeeRepository
{
    Task<Payee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payee>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default
    );
    Task<Payee?> FindByAliasAsync(
        string rawPayee,
        Guid ownerId,
        CancellationToken cancellationToken = default
    );
    Task<Payee> AddAsync(Payee payee, CancellationToken cancellationToken = default);
    Task<Payee> UpdateAsync(Payee payee, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasLinkedTransactionsAsync(Guid id, CancellationToken cancellationToken = default);
}
