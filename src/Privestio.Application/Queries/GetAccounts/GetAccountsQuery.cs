using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAccounts;

public record GetAccountsQuery(Guid OwnerId) : IRequest<IReadOnlyList<AccountResponse>>;
