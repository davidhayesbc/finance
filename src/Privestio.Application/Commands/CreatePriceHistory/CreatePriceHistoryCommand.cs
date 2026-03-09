using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreatePriceHistory;

public record CreatePriceHistoryCommand(IReadOnlyList<PriceHistoryEntry> Entries)
    : IRequest<IReadOnlyList<PriceHistoryResponse>>;

public record PriceHistoryEntry(
    string Symbol,
    decimal Price,
    string Currency,
    DateOnly AsOfDate,
    string Source
);
