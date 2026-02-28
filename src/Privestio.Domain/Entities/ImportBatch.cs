namespace Privestio.Domain.Entities;

/// <summary>
/// Tracks an import batch for audit and rollback purposes.
/// </summary>
public class ImportBatch : BaseEntity
{
    private ImportBatch() { }

    public ImportBatch(
        string fileName,
        string fileFormat,
        Guid userId,
        Guid? mappingId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileFormat);

        FileName = fileName.Trim();
        FileFormat = fileFormat.Trim();
        UserId = userId;
        MappingId = mappingId;
        ImportDate = DateTime.UtcNow;
        Status = ImportStatus.Pending;
    }

    public string FileName { get; private set; } = string.Empty;
    public string FileFormat { get; private set; } = string.Empty;
    public DateTime ImportDate { get; private set; }
    public int RowCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int DuplicateCount { get; set; }
    public ImportStatus Status { get; set; }
    public string? ErrorDetails { get; set; }

    public Guid UserId { get; private set; }
    public User? User { get; set; }

    public Guid? MappingId { get; set; }

    public void Complete(int rowCount, int successCount, int errorCount, int duplicateCount)
    {
        RowCount = rowCount;
        SuccessCount = successCount;
        ErrorCount = errorCount;
        DuplicateCount = duplicateCount;
        Status = errorCount > 0 ? ImportStatus.CompletedWithErrors : ImportStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string errorDetails)
    {
        Status = ImportStatus.Failed;
        ErrorDetails = errorDetails;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ImportStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    CompletedWithErrors = 3,
    Failed = 4,
    RolledBack = 5,
}
