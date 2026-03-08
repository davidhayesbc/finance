// Privestio .NET Aspire AppHost (Task 1.2)
// Orchestrates the API and PostgreSQL for development.

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
var postgres = builder.AddPostgres("postgres").WithPgAdmin().WithDataVolume();

var privestioDb = postgres.AddDatabase("privestio");

// API project - referenced by path (non-workload Aspire 9.x style)
var api = builder
    .AddProject("api", "../Privestio.Api/Privestio.Api.csproj")
    .WithReference(privestioDb)
    .WaitFor(privestioDb);

// Web (Blazor WASM PWA)
builder.AddProject("web", "../Privestio.Web/Privestio.Web.csproj").WithReference(api).WaitFor(api);

builder.Build().Run();
