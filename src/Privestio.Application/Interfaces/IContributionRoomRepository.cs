using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IContributionRoomRepository
{
    Task<ContributionRoom?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContributionRoom>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );
    Task<ContributionRoom?> GetByAccountIdAndYearAsync(
        Guid accountId,
        int year,
        CancellationToken cancellationToken = default
    );
    Task<ContributionRoom> AddAsync(
        ContributionRoom contributionRoom,
        CancellationToken cancellationToken = default
    );
    Task<ContributionRoom> UpdateAsync(
        ContributionRoom contributionRoom,
        CancellationToken cancellationToken = default
    );
}
