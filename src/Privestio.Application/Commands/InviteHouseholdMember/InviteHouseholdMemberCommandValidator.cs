using FluentValidation;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.InviteHouseholdMember;

public class InviteHouseholdMemberCommandValidator
    : AbstractValidator<InviteHouseholdMemberCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "Member", "Viewer"];

    public InviteHouseholdMemberCommandValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be one of: Admin, Member, Viewer.");
        RuleFor(x => x.InvitedByUserId).NotEmpty();
    }
}
