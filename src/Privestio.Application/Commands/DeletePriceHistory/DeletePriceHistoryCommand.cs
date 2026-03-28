using MediatR;

namespace Privestio.Application.Commands.DeletePriceHistory;

public record DeletePriceHistoryCommand(Guid Id) : IRequest<bool>;
