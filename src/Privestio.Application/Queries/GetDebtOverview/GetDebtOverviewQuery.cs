using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetDebtOverview;

public record GetDebtOverviewQuery(Guid UserId) : IRequest<DebtOverviewResponse>;
