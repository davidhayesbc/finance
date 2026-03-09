using System.Text.Json;
using System.Text.Json.Serialization;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Services;

/// <summary>
/// Exports all user data as JSON for data portability / GDPR compliance.
/// </summary>
public class DataExportService : IDataExportService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public async Task<string> ExportUserDataAsync(
        IUnitOfWork uow,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var accounts = await uow.Accounts.GetByOwnerIdAsync(userId, ct);

        var transactions = await uow.Transactions.GetByOwnerAndDateRangeAsync(
            userId,
            DateTime.MinValue,
            DateTime.MaxValue,
            ct
        );

        var exportData = new
        {
            exportedAt = DateTime.UtcNow,
            userId,
            accounts = accounts.Select(a => new
            {
                a.Id,
                a.Name,
                accountType = a.AccountType.ToString(),
                accountSubType = a.AccountSubType.ToString(),
                a.Currency,
                a.Institution,
                a.AccountNumber,
                openingBalance = a.OpeningBalance.Amount,
                currentBalance = a.CurrentBalance.Amount,
                a.IsActive,
                a.IsShared,
                a.Notes,
                a.CreatedAt,
            }),
            transactions = transactions.Select(t => new
            {
                t.Id,
                t.AccountId,
                t.Date,
                amount = t.Amount.Amount,
                t.Description,
                type = t.Type.ToString(),
                t.CategoryId,
                t.PayeeId,
                t.IsReconciled,
                t.Notes,
                t.ExternalId,
                t.CreatedAt,
            }),
        };

        return JsonSerializer.Serialize(exportData, s_jsonOptions);
    }
}
