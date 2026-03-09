using Privestio.Application.Interfaces;

namespace Privestio.Application.Services;

/// <summary>
/// Provides data export capabilities for user data (GDPR / data portability).
/// </summary>
public interface IDataExportService
{
    /// <summary>
    /// Exports all user data (accounts and transactions) as a JSON string.
    /// </summary>
    Task<string> ExportUserDataAsync(IUnitOfWork uow, Guid userId, CancellationToken ct = default);
}
