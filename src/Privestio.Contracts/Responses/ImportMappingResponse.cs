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
    public List<string> BuyKeywords { get; init; } = [];
    public List<string> SellKeywords { get; init; } = [];
    public List<string> IncomeKeywords { get; init; } = [];
    public List<string> CashEquivalentSymbols { get; init; } = [];
    public List<string> IgnoreRowPatterns { get; init; } = [];
    public bool AmountSignFlipped { get; init; }
    public DateOnly? DefaultDate { get; init; }
}
