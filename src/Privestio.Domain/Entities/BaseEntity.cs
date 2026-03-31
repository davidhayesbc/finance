using Privestio.Domain.Interfaces;

namespace Privestio.Domain.Entities;

/// <summary>
/// Base class for all auditable domain entities with soft-delete support.
/// </summary>
public abstract class BaseEntity : IAuditableEntity
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Legacy column — optimistic concurrency is now handled by PostgreSQL's xmin system column.
    /// Retained temporarily for migration compatibility; will be removed in a future migration.
    /// </summary>
    public long Version { get; set; }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
