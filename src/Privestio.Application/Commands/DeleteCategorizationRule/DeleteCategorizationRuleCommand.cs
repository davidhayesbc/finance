using MediatR;

namespace Privestio.Application.Commands.DeleteCategorizationRule;

public record DeleteCategorizationRuleCommand(Guid Id, Guid UserId) : IRequest<bool>;
