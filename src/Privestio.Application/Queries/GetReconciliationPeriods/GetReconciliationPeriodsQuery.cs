using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetReconciliationPeriods;

public record GetReconciliationPeriodsQuery(Guid AccountId, Guid UserId)
    : IRequest<IReadOnlyList<ReconciliationPeriodResponse>>;
