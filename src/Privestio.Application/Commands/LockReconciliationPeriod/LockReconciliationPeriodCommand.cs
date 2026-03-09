using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.LockReconciliationPeriod;

public record LockReconciliationPeriodCommand(Guid Id, Guid UserId)
    : IRequest<ReconciliationPeriodResponse>;
