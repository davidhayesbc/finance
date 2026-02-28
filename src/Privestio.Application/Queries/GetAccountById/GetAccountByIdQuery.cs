using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAccountById;

public record GetAccountByIdQuery(Guid AccountId, Guid RequestingUserId) : IRequest<AccountResponse?>;
