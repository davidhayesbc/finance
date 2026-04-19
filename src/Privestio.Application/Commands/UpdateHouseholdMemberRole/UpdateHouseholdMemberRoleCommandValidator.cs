using FluentValidation;

namespace Privestio.Application.Commands.UpdateHouseholdMemberRole;

public class UpdateHouseholdMemberRoleCommandValidator
    : AbstractValidator<UpdateHouseholdMemberRoleCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "Member", "Viewer"];

    public UpdateHouseholdMemberRoleCommandValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole)
            .NotEmpty()
            .Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be one of: Admin, Member, Viewer.");
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
