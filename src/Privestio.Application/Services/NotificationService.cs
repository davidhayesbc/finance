using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Services;

public class NotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CheckMinimumBalanceAlerts(
        Guid userId,
        decimal minimumBalance,
        CancellationToken cancellationToken = default
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(userId, cancellationToken);

        foreach (
            var account in accounts.Where(a =>
                a.AccountType == AccountType.Banking && a.CurrentBalance.Amount < minimumBalance
            )
        )
        {
            var notification = new Notification(
                userId,
                "MinimumBalance",
                NotificationSeverity.Warning,
                $"Low balance: {account.Name}",
                $"Account '{account.Name}' balance ({account.CurrentBalance}) is below the minimum threshold of {minimumBalance:F2} {account.Currency}.",
                "Account",
                account.Id
            );

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CheckBudgetOverageAlerts(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default
    )
    {
        var budgets = await _unitOfWork.Budgets.GetByUserIdAndPeriodAsync(
            userId,
            year,
            month,
            cancellationToken
        );

        if (budgets.Count == 0)
            return;

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var transactions = await _unitOfWork.Transactions.GetByOwnerAndDateRangeAsync(
            userId,
            startDate,
            endDate,
            cancellationToken
        );

        var actualByCategory = CalculateActualSpending(transactions);

        foreach (var budget in budgets)
        {
            actualByCategory.TryGetValue(budget.CategoryId, out var actual);
            if (actual <= budget.Amount.Amount)
                continue;

            var notification = new Notification(
                userId,
                "BudgetOverage",
                NotificationSeverity.Warning,
                $"Over budget: {budget.Category?.Name ?? "Unknown"}",
                $"Spending in '{budget.Category?.Name}' ({actual:F2}) exceeds the budget of {budget.Amount.Amount:F2} {budget.Amount.CurrencyCode} for {year}-{month:D2}.",
                "Budget",
                budget.Id
            );

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CheckSinkingFundAlerts(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var funds = await _unitOfWork.SinkingFunds.GetActiveByUserIdAsync(
            userId,
            cancellationToken
        );
        var now = DateTime.UtcNow;

        foreach (var fund in funds)
        {
            if (fund.IsOnTrack(now))
                continue;

            var notification = new Notification(
                userId,
                "SinkingFundBehind",
                NotificationSeverity.Warning,
                $"Behind schedule: {fund.Name}",
                $"Sinking fund '{fund.Name}' is behind schedule. Accumulated: {fund.AccumulatedAmount}, Target: {fund.TargetAmount}, Due: {fund.DueDate:yyyy-MM-dd}.",
                "SinkingFund",
                fund.Id
            );

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<Guid, decimal> CalculateActualSpending(
        IReadOnlyList<Transaction> transactions
    )
    {
        var actualByCategory = new Dictionary<Guid, decimal>();

        foreach (var txn in transactions)
        {
            if (txn.Type == TransactionType.Transfer)
                continue;

            if (txn.IsSplit)
            {
                foreach (var split in txn.Splits.Where(s => !s.IsDeleted))
                {
                    if (!actualByCategory.TryGetValue(split.CategoryId, out var current))
                        current = 0m;
                    actualByCategory[split.CategoryId] = current + Math.Abs(split.Amount.Amount);
                }
            }
            else if (txn.CategoryId.HasValue)
            {
                if (!actualByCategory.TryGetValue(txn.CategoryId.Value, out var current))
                    current = 0m;
                actualByCategory[txn.CategoryId.Value] = current + Math.Abs(txn.Amount.Amount);
            }
        }

        return actualByCategory;
    }
}
