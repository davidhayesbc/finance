using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.CreateCategorizationRule;

public class CreateCategorizationRuleCommandHandler
    : IRequestHandler<CreateCategorizationRuleCommand, CategorizationRuleResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategorizationRuleCommandHandler(IUnitOfWork unitOfWork) =>
        _unitOfWork = unitOfWork;

    public async Task<CategorizationRuleResponse> Handle(
        CreateCategorizationRuleCommand request,
        CancellationToken cancellationToken
    )
    {
        var rule = new CategorizationRule(
            request.Name,
            request.Priority,
            request.Conditions,
            request.Actions,
            request.UserId
        );

        if (!request.IsEnabled)
            rule.Disable();

        await _unitOfWork.CategorizationRules.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategorizationRuleResponse
        {
            Id = rule.Id,
            Name = rule.Name,
            Priority = rule.Priority,
            Conditions = rule.Conditions,
            Actions = rule.Actions,
            IsEnabled = rule.IsEnabled,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt,
        };
    }
}
