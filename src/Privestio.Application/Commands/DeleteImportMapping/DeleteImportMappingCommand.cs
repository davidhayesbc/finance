using MediatR;

namespace Privestio.Application.Commands.DeleteImportMapping;

public record DeleteImportMappingCommand(Guid Id, Guid UserId) : IRequest<bool>;
