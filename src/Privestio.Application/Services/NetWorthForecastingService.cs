using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Services;

public class NetWorthForecastingService
{
    public async Task<NetWorthForecastResponse> ProjectNetWorth(
        IUnitOfWork uow,
        Guid userId,
        Guid scenarioId,
        int months,
        CancellationToken ct
    )
    {
        var accounts = await uow.Accounts.GetByOwnerIdAsync(userId, ct);
        var activeAccounts = accounts.Where(a => a.IsActive).ToList();

        var scenario =
            await uow.ForecastScenarios.GetByIdAsync(scenarioId, ct)
            ?? throw new KeyNotFoundException($"ForecastScenario {scenarioId} not found.");

        if (scenario.UserId != userId)
            throw new UnauthorizedAccessException("Cannot use another user's forecast scenario.");

        var recurringTransactions = await uow.RecurringTransactions.GetActiveByUserIdAsync(
            userId,
            ct
        );

        var assetTypes = new HashSet<AccountType>
        {
            AccountType.Banking,
            AccountType.Investment,
            AccountType.Property,
        };

        var liabilityTypes = new HashSet<AccountType> { AccountType.Credit, AccountType.Loan };

        // Build a lookup of growth rates: first by account id, then by account type
        var assumptionsByAccountId = scenario
            .GrowthAssumptions.Where(g => g.AccountId.HasValue)
            .ToDictionary(g => g.AccountId!.Value);

        var assumptionsByType = scenario
            .GrowthAssumptions.Where(g => g.AccountType.HasValue)
            .ToDictionary(g => g.AccountType!.Value);

        // Calculate monthly recurring income and expenses per account
        var monthlyRecurringByAccount = new Dictionary<Guid, decimal>();
        foreach (var rt in recurringTransactions)
        {
            var monthlyAmount =
                rt.TransactionType == TransactionType.Credit ? rt.Amount.Amount : -rt.Amount.Amount;

            // Normalize to monthly
            var normalizedMonthly = rt.Frequency switch
            {
                RecurrenceFrequency.Weekly => monthlyAmount * 52m / 12m,
                RecurrenceFrequency.BiWeekly => monthlyAmount * 26m / 12m,
                RecurrenceFrequency.Monthly => monthlyAmount,
                RecurrenceFrequency.Quarterly => monthlyAmount / 3m,
                RecurrenceFrequency.SemiAnnually => monthlyAmount / 6m,
                RecurrenceFrequency.Annually => monthlyAmount / 12m,
                _ => monthlyAmount,
            };

            if (monthlyRecurringByAccount.TryGetValue(rt.AccountId, out var existing))
                monthlyRecurringByAccount[rt.AccountId] = existing + normalizedMonthly;
            else
                monthlyRecurringByAccount[rt.AccountId] = normalizedMonthly;
        }

        // Initialize balances
        var balances = activeAccounts.ToDictionary(a => a.Id, a => a.CurrentBalance.Amount);

        var periods = new List<NetWorthForecastPeriod>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var m = 1; m <= months; m++)
        {
            var periodDate = today.AddMonths(m);

            foreach (var account in activeAccounts)
            {
                var balance = balances[account.Id];

                // Apply growth rate
                decimal monthlyGrowthRate = 0;

                if (assumptionsByAccountId.TryGetValue(account.Id, out var accountAssumption))
                {
                    monthlyGrowthRate = accountAssumption.RealGrowthRate / 100m / 12m;
                }
                else if (assumptionsByType.TryGetValue(account.AccountType, out var typeAssumption))
                {
                    monthlyGrowthRate = typeAssumption.RealGrowthRate / 100m / 12m;
                }

                balance += balance * monthlyGrowthRate;

                // Apply recurring transactions
                if (monthlyRecurringByAccount.TryGetValue(account.Id, out var recurring))
                {
                    balance += recurring;
                }

                balances[account.Id] = Math.Round(balance, 2, MidpointRounding.ToEven);
            }

            var projectedAssets = activeAccounts
                .Where(a => assetTypes.Contains(a.AccountType))
                .Sum(a => balances[a.Id]);

            var projectedLiabilities = activeAccounts
                .Where(a => liabilityTypes.Contains(a.AccountType))
                .Sum(a => Math.Abs(balances[a.Id]));

            periods.Add(
                new NetWorthForecastPeriod
                {
                    Date = periodDate,
                    ProjectedAssets = projectedAssets,
                    ProjectedLiabilities = projectedLiabilities,
                    ProjectedNetWorth = projectedAssets - projectedLiabilities,
                }
            );
        }

        var baseCurrency = activeAccounts
            .Select(a => a.Currency)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "CAD";

        return new NetWorthForecastResponse
        {
            Periods = periods,
            ScenarioName = scenario.Name,
            Currency = baseCurrency,
        };
    }
}
