namespace Privestio.Application.Configuration;

public class PricingOptions
{
    public List<string> ProviderOrder { get; set; } = ["YahooFinance", "MsnFinance"];

    /// <summary>
    /// Hour of day (UTC, 0-23) at which the daily background price fetch runs.
    /// Defaults to 18 (6 PM UTC / after North American close).
    /// </summary>
    public int DailyFetchHourUtc { get; set; } = 18;
}
