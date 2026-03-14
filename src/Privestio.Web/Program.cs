using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using Privestio.Web;
using Privestio.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API HttpClient
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5001";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Fluent UI services
builder.Services.AddFluentUIComponents();

// App services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IRuleService, RuleService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<ISinkingFundService, SinkingFundService>();
builder.Services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
builder.Services.AddScoped<INotificationWebService, NotificationWebService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IForecastScenarioService, ForecastScenarioService>();
builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
builder.Services.AddScoped<IValuationService, ValuationService>();
builder.Services.AddScoped<IHoldingService, HoldingService>();
builder.Services.AddScoped<ILotService, LotService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IContributionRoomService, ContributionRoomService>();
builder.Services.AddScoped<IAmortizationService, AmortizationService>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<IIndexedDbService, IndexedDbService>();
builder.Services.AddScoped<IConnectivityService, ConnectivityService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IConflictResolutionWebService, ConflictResolutionWebService>();

var host = builder.Build();

await host.Services.GetRequiredService<IAuthService>().InitializeAsync();

await host.RunAsync();
