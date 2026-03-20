using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Interfaces;
using Privestio.Infrastructure.Data;
using Privestio.Infrastructure.Data.Services;
using Privestio.Infrastructure.ExchangeRates;
using Privestio.Infrastructure.Identity;
using Privestio.Infrastructure.Importers;
using Privestio.Infrastructure.PriceFeeds;
using Privestio.Infrastructure.Rules;

namespace Privestio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("privestio")
            ?? throw new InvalidOperationException("Connection string 'privestio' not found.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<PrivestioDbContext>(options =>
            options.UseNpgsql(
                dataSource,
                npgsql => npgsql.MigrationsAssembly(typeof(PrivestioDbContext).Assembly.FullName)
            )
        );

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password policy (Task 1.6a)
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;

                // Lockout policy (Task 1.6a)
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<PrivestioDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserDataResetService, UserDataResetService>();

        // Ingestion pipeline services (Phase 2)
        services.AddScoped<ITransactionImporter, CsvTransactionImporter>();
        services.AddScoped<ITransactionImporter, OfxTransactionImporter>();
        services.AddScoped<ITransactionImporter, QifTransactionImporter>();
        services.AddScoped<IHoldingsImporter, PdfHoldingsImporter>();
        services.AddSingleton<TransactionFingerprintService>();
        services.AddScoped<IRuleEvaluator, CategorizationRuleEvaluator>();
        services.AddScoped<IFilePreviewService, CsvFilePreviewService>();

        // Price feed providers (Phase 5.4)
        services.Configure<PricingOptions>(configuration.GetSection("Pricing"));

        services.AddHttpClient<YahooFinancePriceFeedProvider>(client =>
        {
            client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Privestio/1.0 (self-hosted personal finance tracker)"
            );
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<MsnFinancePriceFeedProvider>(client =>
        {
            client.BaseAddress = new Uri("https://assets.msn.com/service/Finance/");
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Privestio/1.0 (self-hosted personal finance tracker)"
            );
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IPriceFeedProvider>(sp =>
        {
            var yahoo = sp.GetRequiredService<YahooFinancePriceFeedProvider>();
            var msn = sp.GetRequiredService<MsnFinancePriceFeedProvider>();
            var options = sp.GetRequiredService<IOptions<PricingOptions>>();
            var logger = sp.GetRequiredService<ILogger<ChainedPriceFeedProvider>>();

            var providers = new Dictionary<string, IPriceFeedProvider>
            {
                [yahoo.ProviderName] = yahoo,
                [msn.ProviderName] = msn,
            };

            return new ChainedPriceFeedProvider(providers, options.Value.ProviderOrder, logger);
        });

        // Exchange rate ingestion provider (Phase 5.5a)
        services.AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.frankfurter.app/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations on startup (Task 1.18).
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivestioDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
