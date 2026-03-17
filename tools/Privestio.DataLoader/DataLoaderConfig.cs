using System.Text.Json.Serialization;

namespace Privestio.DataLoader;

public class DataLoaderConfig
{
    public AuthConfig Auth { get; set; } = new();
    public List<CategoryConfig> Categories { get; set; } = [];
    public List<PayeeConfig> Payees { get; set; } = [];
    public List<TagConfig> Tags { get; set; } = [];
    public List<ImportMappingConfig> ImportMappings { get; set; } = [];
    public List<AccountConfig> Accounts { get; set; } = [];
    public List<PriceHistoryGroupConfig> PriceHistory { get; set; } = [];
}

public class AuthConfig
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Register { get; set; }
}

public class CategoryConfig
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public string? ParentCategory { get; set; }
}

public class PayeeConfig
{
    public string DisplayName { get; set; } = string.Empty;
    public string? DefaultCategory { get; set; }
}

public class TagConfig
{
    public string Name { get; set; } = string.Empty;
}

public class ImportMappingConfig
{
    public string Name { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public Dictionary<string, string> ColumnMappings { get; set; } = new();
    public string? DateFormat { get; set; }
    public bool HasHeaderRow { get; set; } = true;
    public string? AmountDebitColumn { get; set; }
    public string? AmountCreditColumn { get; set; }
    public List<string>? BuyKeywords { get; set; }
    public List<string>? SellKeywords { get; set; }
    public List<string>? IncomeKeywords { get; set; }
    public List<string>? CashEquivalentSymbols { get; set; }
    public List<string>? IgnoreRowPatterns { get; set; }
    public bool AmountSignFlipped { get; set; }
    public string? DefaultDate { get; set; }
}

public class AccountConfig
{
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountSubType { get; set; } = string.Empty;
    public string Currency { get; set; } = "CAD";
    public string? Institution { get; set; }
    public string? AccountNumber { get; set; }
    public decimal OpeningBalance { get; set; }
    public string OpeningDate { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<ImportConfig> Imports { get; set; } = [];
    public List<ValuationConfig> Valuations { get; set; } = [];
    public AccountAdjustmentConfig? Adjustments { get; set; }
}

public class ImportConfig
{
    public string File { get; set; } = string.Empty;
    public string? MappingName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ImportPolicyConfig Policy { get; set; } = ImportPolicyConfig.SkipInvalid;
}

public enum ImportPolicyConfig
{
    SkipInvalid = 0,
    FailFast = 1,
}

public class ValuationConfig
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "CAD";
    public string EffectiveDate { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class AccountAdjustmentConfig
{
    public string? Name { get; set; }
    public string? Institution { get; set; }
    public string? Notes { get; set; }
    public bool? IsShared { get; set; }
}

public class PriceHistoryGroupConfig
{
    public string Symbol { get; set; } = string.Empty;
    public string Currency { get; set; } = "CAD";
    public string Source { get; set; } = string.Empty;
    public List<PriceEntryConfig> Prices { get; set; } = [];
}

public class PriceEntryConfig
{
    public string Date { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
