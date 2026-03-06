using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteCategorizationRule;

public class DeleteCategorizationRuleCommandHandler
    : IRequestHandler<DeleteCategorizationRuleCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategorizationRuleCommandHandler(IUnitOfWork unitOfWork) =>
        _unitOfWork = unitOfWork;

    public async Task<bool> Handle(
        DeleteCategorizationRuleCommand request,
        CancellationToken cancellationToken
    )
    {
        var rule = await _unitOfWork.CategorizationRules.GetByIdAsync(
            request.Id,
            cancellationToken
        );
        if (rule is null || rule.UserId != request.UserId)
            return false;

        await _unitOfWork.CategorizationRules.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
