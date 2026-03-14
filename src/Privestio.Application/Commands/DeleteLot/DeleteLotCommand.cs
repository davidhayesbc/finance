using MediatR;

namespace Privestio.Application.Commands.DeleteLot;

public record DeleteLotCommand(Guid LotId, Guid UserId) : IRequest<bool>;
