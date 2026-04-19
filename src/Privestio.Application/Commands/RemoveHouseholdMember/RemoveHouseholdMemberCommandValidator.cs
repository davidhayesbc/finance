using FluentValidation;

namespace Privestio.Application.Commands.RemoveHouseholdMember;

public class RemoveHouseholdMemberCommandValidator
    : AbstractValidator<RemoveHouseholdMemberCommand>
{
    public RemoveHouseholdMemberCommandValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.UserIdToRemove).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
