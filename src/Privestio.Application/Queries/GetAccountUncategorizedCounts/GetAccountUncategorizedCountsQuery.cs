using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAccountUncategorizedCounts;

public record GetAccountUncategorizedCountsQuery(Guid UserId)
    : IRequest<IReadOnlyList<AccountUncategorizedCountResponse>>;
