using FluentValidation;

namespace Privestio.Application.Commands.CreateRecurringTransaction;

public class CreateRecurringTransactionCommandValidator
    : AbstractValidator<CreateRecurringTransactionCommand>
{
    public CreateRecurringTransactionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.TransactionType).NotEmpty();
        RuleFor(x => x.Frequency).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
