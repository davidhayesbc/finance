namespace Privestio.Contracts.Responses;

public record ImportMappingResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FileFormat { get; init; } = string.Empty;
    public string? Institution { get; init; }
    public Dictionary<string, string> ColumnMappings { get; init; } = new();
    public string? DateFormat { get; init; }
    public bool HasHeaderRow { get; init; }
    public string? AmountDebitColumn { get; init; }
    public string? AmountCreditColumn { get; init; }
}
