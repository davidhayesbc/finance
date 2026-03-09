using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UnlockReconciliationPeriod;

public record UnlockReconciliationPeriodCommand(Guid Id, Guid UserId, string Reason)
    : IRequest<ReconciliationPeriodResponse>;
