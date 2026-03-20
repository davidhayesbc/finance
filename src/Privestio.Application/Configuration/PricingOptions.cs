namespace Privestio.Application.Configuration;

public class PricingOptions
{
    public List<string> ProviderOrder { get; set; } = ["YahooFinance", "MsnFinance"];
}
