namespace Privestio.Application.Services;

/// <summary>
/// Clears loader-managed user data so manifests can be re-imported from a clean slate.
/// </summary>
public interface IUserDataResetService
{
    Task<UserDataResetResult> ClearLoaderDataAsync(Guid userId, CancellationToken ct = default);
}

public sealed record UserDataResetResult(
    int Accounts,
    int Transactions,
    int TransactionSplits,
    int Holdings,
    int Lots,
    int Valuations,
    int Categories,
    int Payees,
    int Tags,
    int ImportMappings,
    int Budgets,
    int SinkingFunds,
    int RecurringTransactions,
    int Rules,
    int ForecastScenarios,
    int Notifications,
    int ImportBatches
)
{
    public int Total =>
        Accounts
        + Transactions
        + TransactionSplits
        + Holdings
        + Lots
        + Valuations
        + Categories
        + Payees
        + Tags
        + ImportMappings
        + Budgets
        + SinkingFunds
        + RecurringTransactions
        + Rules
        + ForecastScenarios
        + Notifications
        + ImportBatches;
}
