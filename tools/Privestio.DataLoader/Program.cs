using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Privestio.DataLoader;

var manifestPath =
    GetArg(args, "--manifest") ?? Environment.GetEnvironmentVariable("PRIVESTIO_MANIFEST");
var dataDir =
    GetArg(args, "--data-dir") ?? Environment.GetEnvironmentVariable("PRIVESTIO_DATA_DIR");
var apiUrl =
    GetArg(args, "--api-url")
    ?? Environment.GetEnvironmentVariable("PRIVESTIO_API_URL")
    ?? "https://localhost:7292";
var dryRun = args.Contains("--dry-run");
var verboseImportErrors =
    args.Contains("--verbose-import-errors")
    || string.Equals(
        Environment.GetEnvironmentVariable("PRIVESTIO_VERBOSE_IMPORT_ERRORS"),
        "true",
        StringComparison.OrdinalIgnoreCase
    );

if (string.IsNullOrEmpty(manifestPath))
{
    Console.Error.WriteLine("Error: --manifest path is required (or set PRIVESTIO_MANIFEST)");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage: dotnet run --project tools/Privestio.DataLoader -- \\");
    Console.Error.WriteLine("  --manifest /path/to/manifest.json \\");
    Console.Error.WriteLine("  --data-dir /path/to/data \\");
    Console.Error.WriteLine("  [--api-url https://localhost:7292] \\");
    Console.Error.WriteLine("  [--dry-run] \\");
    Console.Error.WriteLine("  [--verbose-import-errors]");
    return 1;
}

if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine($"Error: Manifest file not found: {manifestPath}");
    return 1;
}

if (string.IsNullOrEmpty(dataDir))
{
    dataDir = Path.GetDirectoryName(manifestPath) ?? ".";
}

if (!Directory.Exists(dataDir))
{
    Console.Error.WriteLine($"Error: Data directory not found: {dataDir}");
    return 1;
}

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var sp = services.BuildServiceProvider();

var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<LoaderOrchestrator>();
var apiLogger = loggerFactory.CreateLogger<ApiClient>();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    Converters = { new JsonStringEnumConverter() },
};

var manifestJson = await File.ReadAllTextAsync(manifestPath);
var config =
    JsonSerializer.Deserialize<DataLoaderConfig>(manifestJson, jsonOptions)
    ?? throw new InvalidOperationException("Failed to deserialize manifest");

logger.LogInformation("Manifest loaded: {Path}", manifestPath);
logger.LogInformation("Data directory: {DataDir}", dataDir);
logger.LogInformation("API URL: {ApiUrl}", apiUrl);
logger.LogInformation(
    "Verbose import errors: {Enabled}",
    verboseImportErrors ? "enabled" : "disabled"
);

using var api = new ApiClient(apiUrl, apiLogger);
var orchestrator = new LoaderOrchestrator(
    api,
    config,
    dataDir,
    dryRun,
    verboseImportErrors,
    logger
);
return await orchestrator.RunAsync();

static string? GetArg(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
