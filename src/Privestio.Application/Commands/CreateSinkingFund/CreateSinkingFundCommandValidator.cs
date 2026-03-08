using FluentValidation;

namespace Privestio.Application.Commands.CreateSinkingFund;

public class CreateSinkingFundCommandValidator : AbstractValidator<CreateSinkingFundCommand>
{
    public CreateSinkingFundCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TargetAmount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
