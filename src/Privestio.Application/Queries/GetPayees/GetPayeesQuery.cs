using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPayees;

public record GetPayeesQuery(Guid OwnerId) : IRequest<IReadOnlyList<PayeeResponse>>;
