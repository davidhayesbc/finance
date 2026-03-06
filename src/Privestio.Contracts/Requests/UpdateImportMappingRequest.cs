namespace Privestio.Contracts.Requests;

public record UpdateImportMappingRequest
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, string> ColumnMappings { get; init; } = new();
    public string? DateFormat { get; init; }
    public bool HasHeaderRow { get; init; } = true;
    public string? AmountDebitColumn { get; init; }
    public string? AmountCreditColumn { get; init; }
}
