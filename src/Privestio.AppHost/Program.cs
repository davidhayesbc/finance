// Privestio .NET Aspire AppHost (Task 1.2)
// Orchestrates the API and PostgreSQL for development.

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var privestioDb = postgres.AddDatabase("privestio");

// API project - referenced by path (non-workload Aspire 9.x style)
builder.AddProject("api", "../Privestio.Api/Privestio.Api.csproj")
    .WithReference(privestioDb)
    .WaitFor(privestioDb);

builder.Build().Run();
