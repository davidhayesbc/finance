using FluentValidation;

namespace Privestio.Application.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private static readonly string[] ValidAccountTypes = ["Banking", "Credit", "Investment", "Property", "Loan"];
    private static readonly string[] ValidSubTypes = [
        "Chequing", "Savings", "CreditCard", "LineOfCredit",
        "RRSP", "TFSA", "RESP", "LIRA", "NonRegistered",
        "RealEstate", "Vehicle", "OtherAsset",
        "Mortgage", "AutoLoan", "StudentLoan", "PersonalLoan",
    ];

    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.AccountType)
            .NotEmpty()
            .Must(t => ValidAccountTypes.Contains(t))
            .WithMessage($"AccountType must be one of: {string.Join(", ", ValidAccountTypes)}");

        RuleFor(x => x.AccountSubType)
            .NotEmpty()
            .Must(t => ValidSubTypes.Contains(t))
            .WithMessage($"AccountSubType must be one of: {string.Join(", ", ValidSubTypes)}");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO code (e.g., CAD, USD).");

        RuleFor(x => x.OpeningDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Opening date cannot be in the future.");

        RuleFor(x => x.OwnerId)
            .NotEmpty();
    }
}
