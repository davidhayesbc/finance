using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateCategorizationRule;

public record CreateCategorizationRuleCommand(
    string Name,
    int Priority,
    string Conditions,
    string Actions,
    Guid UserId,
    bool IsEnabled = true
) : IRequest<CategorizationRuleResponse>;
