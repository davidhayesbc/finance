using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateCategorizationRule;

public class UpdateCategorizationRuleCommandHandler
    : IRequestHandler<UpdateCategorizationRuleCommand, CategorizationRuleResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategorizationRuleCommandHandler(IUnitOfWork unitOfWork) =>
        _unitOfWork = unitOfWork;

    public async Task<CategorizationRuleResponse?> Handle(
        UpdateCategorizationRuleCommand request,
        CancellationToken cancellationToken
    )
    {
        var rule = await _unitOfWork.CategorizationRules.GetByIdAsync(
            request.Id,
            cancellationToken
        );
        if (rule is null || rule.UserId != request.UserId)
            return null;

        rule.Rename(request.Name);
        rule.Priority = request.Priority;
        rule.Conditions = request.Conditions;
        rule.Actions = request.Actions;

        if (request.IsEnabled)
            rule.Enable();
        else
            rule.Disable();

        await _unitOfWork.CategorizationRules.UpdateAsync(rule, cancellationToken);
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
