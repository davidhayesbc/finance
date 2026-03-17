using Microsoft.EntityFrameworkCore;
using Privestio.Application.Services;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Services;

public class UserDataResetService : IUserDataResetService
{
    private readonly PrivestioDbContext _dbContext;

    public UserDataResetService(PrivestioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDataResetResult> ClearLoaderDataAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var accounts = await _dbContext.Accounts.Where(a => a.OwnerId == userId).ToListAsync(ct);
        var accountIds = accounts.Select(a => a.Id).ToList();

        var transactions =
            accountIds.Count == 0
                ? []
                : await _dbContext
                    .Transactions.Where(t => accountIds.Contains(t.AccountId))
                    .ToListAsync(ct);
        var transactionIds = transactions.Select(t => t.Id).ToList();

        var transactionSplits =
            transactionIds.Count == 0
                ? []
                : await _dbContext
                    .TransactionSplits.Where(s => transactionIds.Contains(s.TransactionId))
                    .ToListAsync(ct);

        var holdings =
            accountIds.Count == 0
                ? []
                : await _dbContext
                    .Holdings.Where(h => accountIds.Contains(h.AccountId))
                    .ToListAsync(ct);
        var holdingIds = holdings.Select(h => h.Id).ToList();

        var lots =
            holdingIds.Count == 0
                ? []
                : await _dbContext
                    .Lots.Where(l => holdingIds.Contains(l.HoldingId))
                    .ToListAsync(ct);

        var valuations =
            accountIds.Count == 0
                ? []
                : await _dbContext
                    .Valuations.Where(v => accountIds.Contains(v.AccountId))
                    .ToListAsync(ct);

        var categories = await _dbContext
            .Categories.Where(c => c.OwnerId == userId)
            .ToListAsync(ct);
        var payees = await _dbContext.Payees.Where(p => p.OwnerId == userId).ToListAsync(ct);
        var tags = await _dbContext.Tags.Where(t => t.OwnerId == userId).ToListAsync(ct);
        var importMappings = await _dbContext
            .ImportMappings.Where(m => m.UserId == userId)
            .ToListAsync(ct);
        var budgets = await _dbContext.Budgets.Where(b => b.UserId == userId).ToListAsync(ct);
        var sinkingFunds = await _dbContext
            .SinkingFunds.Where(s => s.UserId == userId)
            .ToListAsync(ct);
        var recurringTransactions = await _dbContext
            .RecurringTransactions.Where(r => r.UserId == userId)
            .ToListAsync(ct);
        var rules = await _dbContext
            .Set<CategorizationRule>()
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);
        var forecastScenarios = await _dbContext
            .ForecastScenarios.Where(f => f.UserId == userId)
            .ToListAsync(ct);
        var notifications = await _dbContext
            .Notifications.Where(n => n.UserId == userId)
            .ToListAsync(ct);
        var importBatches = await _dbContext
            .ImportBatches.Where(i => i.UserId == userId)
            .ToListAsync(ct);

        var accountCount = SoftDeleteRange(accounts);
        var transactionCount = SoftDeleteRange(transactions);
        var transactionSplitCount = SoftDeleteRange(transactionSplits);
        var holdingCount = SoftDeleteRange(holdings);
        var lotCount = SoftDeleteRange(lots);
        var valuationCount = SoftDeleteRange(valuations);
        var categoryCount = SoftDeleteRange(categories);
        var payeeCount = SoftDeleteRange(payees);
        var tagCount = SoftDeleteRange(tags);
        var mappingCount = SoftDeleteRange(importMappings);
        var budgetCount = SoftDeleteRange(budgets);
        var sinkingFundCount = SoftDeleteRange(sinkingFunds);
        var recurringCount = SoftDeleteRange(recurringTransactions);
        var ruleCount = SoftDeleteRange(rules);
        var forecastScenarioCount = SoftDeleteRange(forecastScenarios);
        var notificationCount = SoftDeleteRange(notifications);
        var importBatchCount = SoftDeleteRange(importBatches);

        await _dbContext.SaveChangesAsync(ct);

        return new UserDataResetResult(
            accountCount,
            transactionCount,
            transactionSplitCount,
            holdingCount,
            lotCount,
            valuationCount,
            categoryCount,
            payeeCount,
            tagCount,
            mappingCount,
            budgetCount,
            sinkingFundCount,
            recurringCount,
            ruleCount,
            forecastScenarioCount,
            notificationCount,
            importBatchCount
        );
    }

    private static int SoftDeleteRange<T>(IEnumerable<T> entities)
        where T : BaseEntity
    {
        var count = 0;

        foreach (var entity in entities)
        {
            if (entity.IsDeleted)
            {
                continue;
            }

            entity.SoftDelete();
            count++;
        }

        return count;
    }
}
