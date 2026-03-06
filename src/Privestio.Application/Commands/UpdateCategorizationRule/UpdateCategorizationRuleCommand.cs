using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateCategorizationRule;

public record UpdateCategorizationRuleCommand(
    Guid Id,
    string Name,
    int Priority,
    string Conditions,
    string Actions,
    Guid UserId,
    bool IsEnabled = true
) : IRequest<CategorizationRuleResponse?>;
