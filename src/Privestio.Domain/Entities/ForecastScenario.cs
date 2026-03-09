using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// A named net worth forecast scenario with growth rate assumptions per account/asset class.
/// </summary>
public class ForecastScenario : BaseEntity
{
    private readonly List<GrowthAssumption> _growthAssumptions = new();

    private ForecastScenario() { }

    public ForecastScenario(Guid userId, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        UserId = userId;
        Name = name.Trim();
        Description = description;
        IsDefault = false;
    }

    public Guid UserId { get; private set; }
    public User? User { get; set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }

    public IReadOnlyList<GrowthAssumption> GrowthAssumptions => _growthAssumptions.AsReadOnly();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAssumptions(IEnumerable<GrowthAssumption> assumptions)
    {
        _growthAssumptions.Clear();
        _growthAssumptions.AddRange(assumptions);
        UpdatedAt = DateTime.UtcNow;
    }
}
