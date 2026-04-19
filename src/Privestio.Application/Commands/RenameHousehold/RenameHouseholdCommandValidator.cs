using FluentValidation;

namespace Privestio.Application.Commands.RenameHousehold;

public class RenameHouseholdCommandValidator : AbstractValidator<RenameHouseholdCommand>
{
    public RenameHouseholdCommandValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
