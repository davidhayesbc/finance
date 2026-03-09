using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetSpendingAnalysis;

public record GetSpendingAnalysisQuery(Guid UserId, DateOnly StartDate, DateOnly EndDate)
    : IRequest<SpendingAnalysisResponse>;
