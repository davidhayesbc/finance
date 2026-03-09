using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAmortizationSchedule;

public record GetAmortizationScheduleQuery(Guid AccountId, Guid UserId)
    : IRequest<AmortizationScheduleResponse>;
