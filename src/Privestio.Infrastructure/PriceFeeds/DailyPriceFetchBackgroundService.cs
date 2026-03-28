using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Infrastructure.PriceFeeds;

/// <summary>
/// Background service that fetches the latest price for every non-cash security once per day.
/// Securities whose price feed source is manual-only (PDF statements) are skipped automatically
/// because no provider alias will exist for them — the provider returns an empty quote list and
/// nothing is persisted.
/// </summary>
public sealed class DailyPriceFetchBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyPriceFetchBackgroundService> _logger;
    private readonly PricingOptions _options;

    public DailyPriceFetchBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyPriceFetchBackgroundService> logger,
        IOptions<PricingOptions> options
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DailyPriceFetchBackgroundService started. Daily fetch scheduled at {Hour:D2}:00 UTC.",
            _options.DailyFetchHourUtc
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun();
            _logger.LogDebug(
                "Next daily price fetch in {Minutes} minutes.",
                (int)delay.TotalMinutes
            );

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunFetchAsync(stoppingToken);
        }

        _logger.LogInformation("DailyPriceFetchBackgroundService stopped.");
    }

    private TimeSpan ComputeDelayUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var next = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            _options.DailyFetchHourUtc,
            0,
            0,
            DateTimeKind.Utc
        );
        if (next <= now)
            next = next.AddDays(1);

        return next - now;
    }

    private async Task RunFetchAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily price fetch starting.");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var priceFeedProvider = scope.ServiceProvider.GetRequiredService<IPriceFeedProvider>();
            var resolutionService =
                scope.ServiceProvider.GetRequiredService<SecurityResolutionService>();
            var pricingOptions = scope
                .ServiceProvider.GetRequiredService<IOptions<PricingOptions>>()
                .Value;

            var allSecurities = await unitOfWork.Securities.GetAllNonCashAsync(stoppingToken);
            if (allSecurities.Count == 0)
            {
                _logger.LogInformation("Daily price fetch: no non-cash securities found.");
                return;
            }

            var lookups = allSecurities
                .Select(s =>
                {
                    var order = s.PricingProviderOrder ?? pricingOptions.ProviderOrder;
                    return resolutionService.BuildPriceLookup(s, order);
                })
                .ToList();

            var inserted = 0;
            var skipped = 0;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var allQuotes = await priceFeedProvider.GetLatestPricesAsync(lookups, stoppingToken);

            foreach (var quote in allQuotes.Where(q => q.Price > 0m))
            {
                var alreadyExists = await unitOfWork.PriceHistories.ExistsBySecurityIdAndDateAsync(
                    quote.SecurityId,
                    quote.AsOfDate,
                    stoppingToken
                );

                if (alreadyExists)
                {
                    skipped++;
                    continue;
                }

                var security = allSecurities.FirstOrDefault(s => s.Id == quote.SecurityId);
                if (security is null)
                {
                    skipped++;
                    continue;
                }

                var entry = new PriceHistory(
                    quote.SecurityId,
                    security.DisplaySymbol,
                    quote.Symbol,
                    new Money(quote.Price, quote.Currency),
                    quote.AsOfDate,
                    quote.Source ?? priceFeedProvider.ProviderName
                );

                await unitOfWork.PriceHistories.AddAsync(entry, stoppingToken);
                inserted++;
            }

            if (inserted > 0)
                await unitOfWork.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Daily price fetch complete. Securities={Count} Inserted={Inserted} Skipped={Skipped}",
                allSecurities.Count,
                inserted,
                skipped
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Daily price fetch cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily price fetch failed with an unhandled exception.");
        }
    }
}
