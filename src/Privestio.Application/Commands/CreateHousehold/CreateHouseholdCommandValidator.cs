using FluentValidation;

namespace Privestio.Application.Commands.CreateHousehold;

public class CreateHouseholdCommandValidator : AbstractValidator<CreateHouseholdCommand>
{
    public CreateHouseholdCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerId).NotEmpty();
    }
}
