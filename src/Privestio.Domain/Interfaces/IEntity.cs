namespace Privestio.Domain.Interfaces;

/// <summary>
/// Base interface for all domain entities.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}
