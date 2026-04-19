using FluentValidation;

namespace Privestio.Application.Commands.AcceptHouseholdInvitation;

public class AcceptHouseholdInvitationCommandValidator
    : AbstractValidator<AcceptHouseholdInvitationCommand>
{
    public AcceptHouseholdInvitationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.AcceptingUserId).NotEmpty();
        RuleFor(x => x.AcceptingEmail).NotEmpty().EmailAddress().MaximumLength(254);
    }
}
