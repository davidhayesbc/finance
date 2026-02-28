namespace Privestio.Domain.Interfaces;

/// <summary>
/// Interface for entities that track creation and modification timestamps.
/// </summary>
public interface IAuditableEntity : IEntity
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}
