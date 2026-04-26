using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using System.Text;

// Privestio .NET Aspire AppHost
// Orchestrates the API, PostgreSQL, and Ollama for development.

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
var postgres = builder.AddPostgres("postgres").WithPgAdmin().WithDataVolume();
var privestioDb = postgres.AddDatabase("privestio");

// Ollama (local LLM for AI rule suggestions)
// Dynamically load model profiles from Privestio.Api appsettings.
var apiSettingsDirectory = ResolveApiSettingsDirectory(builder.Environment.ContentRootPath);
var apiSettings = new ConfigurationBuilder()
    .SetBasePath(apiSettingsDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var configuredDefaultProfile = apiSettings["Ollama:DefaultProfile"];
var fallbackModel = apiSettings["Ollama:Model"];

var profileModels = apiSettings
    .GetSection("Ollama:Profiles")
    .GetChildren()
    .Select(section => (ProfileName: section.Key, Model: section["Model"]))
    .Where(x => !string.IsNullOrWhiteSpace(x.Model))
    .Select(x => (x.ProfileName, Model: x.Model!))
    .ToList();

if (profileModels.Count == 0)
{
    var model = string.IsNullOrWhiteSpace(fallbackModel) ? "qwen2.5:7b" : fallbackModel;
    profileModels.Add((
        ProfileName: string.IsNullOrWhiteSpace(configuredDefaultProfile) ? "Balanced" : configuredDefaultProfile,
        Model: model));
}

var defaultProfileName = string.IsNullOrWhiteSpace(configuredDefaultProfile)
    ? profileModels[0].ProfileName
    : configuredDefaultProfile;

var defaultModel = profileModels
    .Where(x => string.Equals(x.ProfileName, defaultProfileName, StringComparison.OrdinalIgnoreCase))
    .Select(x => x.Model)
    .FirstOrDefault();

if (string.IsNullOrWhiteSpace(defaultModel))
{
    defaultModel = string.IsNullOrWhiteSpace(fallbackModel) ? profileModels[0].Model : fallbackModel;
}

var ollama = builder.AddOllama("ollama").WithDataVolume();
var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var modelResources = new Dictionary<string, IResourceBuilder<OllamaModelResource>>(StringComparer.OrdinalIgnoreCase);

foreach (var model in profileModels.Select(x => x.Model!).Distinct(StringComparer.OrdinalIgnoreCase))
{
    var resourceName = BuildModelResourceName(model, resourceNames);
    var modelResource = ollama.AddModel(resourceName, model);
    modelResources[model] = modelResource;
}

if (!modelResources.TryGetValue(defaultModel!, out var defaultModelResource))
{
    defaultModelResource = modelResources.Values.First();
}

// API
var api = builder
    .AddProject("api", "../Privestio.Api/Privestio.Api.csproj")
    .WithReference(privestioDb)
    .WithEnvironment("Ollama__BaseUrl", ollama.GetEndpoint("http"))
    .WithEnvironment("Ollama__DefaultProfile", defaultProfileName)
    .WaitFor(privestioDb);

foreach (var modelResource in modelResources.Values)
{
    api = api.WithReference(modelResource);
}

api = api.WaitFor(defaultModelResource);

// Web (Blazor WASM PWA)
builder
    .AddProject("web", "../Privestio.Web/Privestio.Web.csproj")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();

static string BuildModelResourceName(string model, HashSet<string> usedNames)
{
    var sanitized = new StringBuilder(model.Length);

    foreach (var ch in model)
    {
        sanitized.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
    }

    var baseName = $"model-{sanitized}";
    while (baseName.Contains("--", StringComparison.Ordinal))
    {
        baseName = baseName.Replace("--", "-", StringComparison.Ordinal);
    }

    baseName = baseName.Trim('-');
    if (string.IsNullOrWhiteSpace(baseName))
    {
        baseName = "model-default";
    }

    var candidate = baseName;
    var suffix = 2;
    while (!usedNames.Add(candidate))
    {
        candidate = $"{baseName}-{suffix}";
        suffix++;
    }

    return candidate;
}

static string ResolveApiSettingsDirectory(string contentRootPath)
{
    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(contentRootPath, "../Privestio.Api")),
        Path.GetFullPath(Path.Combine(contentRootPath, "src/Privestio.Api")),
        Path.GetFullPath(Path.Combine(contentRootPath, "Privestio.Api")),
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(Path.Combine(candidate, "appsettings.json")))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException($"Could not locate Privestio.Api appsettings.json from content root '{contentRootPath}'.");
}
