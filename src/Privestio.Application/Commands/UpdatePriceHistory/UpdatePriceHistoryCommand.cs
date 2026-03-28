using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdatePriceHistory;

public record UpdatePriceHistoryCommand(Guid Id, decimal Price, string Currency)
    : IRequest<PriceHistoryResponse?>;
