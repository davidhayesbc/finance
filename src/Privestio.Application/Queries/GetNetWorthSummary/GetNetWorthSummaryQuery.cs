using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetNetWorthSummary;

public record GetNetWorthSummaryQuery(Guid UserId) : IRequest<NetWorthSummaryResponse>;
