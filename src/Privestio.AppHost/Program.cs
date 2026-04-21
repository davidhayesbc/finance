// Privestio .NET Aspire AppHost
// Orchestrates the API, PostgreSQL, and Ollama for development.

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
var postgres = builder.AddPostgres("postgres").WithPgAdmin().WithDataVolume();
var privestioDb = postgres.AddDatabase("privestio");

// Ollama (local LLM for AI rule suggestions)
var ollama = builder.AddOllama("ollama").AddModel("llama3.1:8b");

// API
var api = builder
    .AddProject("api", "../Privestio.Api/Privestio.Api.csproj")
    .WithReference(privestioDb)
    .WithReference(ollama)
    .WithEnvironment("Ollama__BaseUrl", ollama.GetEndpoint("http"))
    .WithEnvironment("Ollama__Model", "llama3.1:8b")
    .WaitFor(privestioDb);

// Web (Blazor WASM PWA)
builder
    .AddProject("web", "../Privestio.Web/Privestio.Web.csproj")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
