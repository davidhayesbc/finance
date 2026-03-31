using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Privestio.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Privestio.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory that uses Testcontainers for a real PostgreSQL instance.
/// Ensures integration tests run against the same database engine as production.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("privestio_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PrivestioDbContext>)
            );
            if (descriptor is not null)
                services.Remove(descriptor);

            // Remove the NpgsqlDataSource registration if present
            var dataSourceDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Npgsql") == true)
                .ToList();
            foreach (var ds in dataSourceDescriptors)
                services.Remove(ds);

            // Register the test database
            services.AddDbContext<PrivestioDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString())
            );
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
