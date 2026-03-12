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
    /// <param name="occurrenceIndex">
    /// Zero for the first occurrence of a given transaction in a batch.
    /// Pass 1, 2, … for subsequent rows that are otherwise identical within the
    /// same batch (e.g. two $5 coffee purchases on the same day). The default
    /// of 0 preserves backward-compatibility with fingerprints already stored.
    /// </param>
    public string GenerateFingerprint(
        Guid accountId,
        DateTime date,
        Money amount,
        string description,
        string? externalId = null,
        int occurrenceIndex = 0
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

        if (occurrenceIndex > 0)
        {
            input += $"|occ{occurrenceIndex}";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }
}
