using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Services;

public class AmortizationScheduleService
{
    public List<AmortizationEntry> GenerateSchedule(
        Guid accountId,
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        decimal monthlyPayment,
        DateOnly startDate,
        string currency
    )
    {
        var entries = new List<AmortizationEntry>();
        var balance = principal;
        var monthlyRate = annualInterestRate / 100m / 12m;

        for (var i = 1; i <= termMonths && balance > 0; i++)
        {
            var interest = Math.Round(balance * monthlyRate, 2, MidpointRounding.ToEven);
            var principalPortion = Math.Round(
                monthlyPayment - interest,
                2,
                MidpointRounding.ToEven
            );
            var actualPayment = monthlyPayment;

            // Final payment adjustment: ensure balance reaches exactly zero
            if (principalPortion >= balance || i == termMonths)
            {
                principalPortion = balance;
                actualPayment = principalPortion + interest;
                balance = 0m;
            }
            else
            {
                balance = Math.Round(balance - principalPortion, 2, MidpointRounding.ToEven);
            }

            var paymentDate = startDate.AddMonths(i - 1);

            var entry = new AmortizationEntry(
                accountId,
                i,
                paymentDate,
                new Money(actualPayment, currency),
                new Money(principalPortion, currency),
                new Money(interest, currency),
                new Money(balance, currency)
            );

            entries.Add(entry);
        }

        return entries;
    }
}
