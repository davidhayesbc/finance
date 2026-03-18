using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetSecurityConflicts;

public record GetSecurityConflictsQuery(Guid UserId) : IRequest<IReadOnlyList<SecurityConflictResponse>>;
