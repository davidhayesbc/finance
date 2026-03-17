using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetNetWorthHistory;

public record GetNetWorthHistoryQuery(Guid UserId, DateOnly FromDate, DateOnly ToDate)
    : IRequest<NetWorthHistoryResponse>;
