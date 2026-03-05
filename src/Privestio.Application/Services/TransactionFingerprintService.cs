using System.Security.Cryptography;
using System.Text;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Services;

/// <summary>
/// Generates stable fingerprints for transactions to enable idempotent import and duplicate detection.
/// </summary>
public class TransactionFingerprintService
{
    /// <summary>
    /// Generates a SHA-256 fingerprint from the core transaction fields.
    /// </summary>
    public string GenerateFingerprint(
        Guid accountId,
        DateTime date,
        Money amount,
        string description,
        string? externalId = null
    )
    {
        var normalizedDescription = description.Trim().ToUpperInvariant();
        var dateString = date.ToString("yyyy-MM-dd");
        var amountString = amount.Amount.ToString("F4");

        var input =
            $"{accountId}|{dateString}|{amountString}|{amount.CurrencyCode}|{normalizedDescription}";

        if (!string.IsNullOrWhiteSpace(externalId))
        {
            input += $"|{externalId.Trim()}";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }
}
