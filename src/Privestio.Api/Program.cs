using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Privestio.Api.Endpoints;
using Privestio.Api.Middleware;
using Privestio.Application;
using Privestio.Infrastructure;
using Serilog;
using Serilog.Events;

// Configure Serilog early (Task 1.16)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Privestio API");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog (Task 1.16)
    builder.Host.UseSerilog((ctx, services, config) =>
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.Console());

    // OpenTelemetry (Task 1.16)
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Privestio.Api"))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();

            if (builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is { } otlpEndpoint)
            {
                tracing.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
            }
        });

    // Infrastructure + Application DI
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // JWT Authentication (Task 1.5)
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key is not configured.");

    if (jwtKey.Length < 32)
        throw new InvalidOperationException("Jwt:Key must be at least 32 characters for HMAC-SHA256 security.");

    if (!builder.Environment.IsDevelopment() && jwtKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Jwt:Key must not use the default placeholder value in non-development environments.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

    // OpenID Connect - Google (Task 1.6)
    if (builder.Configuration["Authentication:Google:ClientId"] is { Length: > 0 } googleClientId)
    {
        builder.Services.AddAuthentication()
            .AddOpenIdConnect("Google", options =>
            {
                options.Authority = "https://accounts.google.com";
                options.ClientId = googleClientId;
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
                options.ResponseType = "code";
                options.SaveTokens = false;
                options.Scope.Add("email");
                options.Scope.Add("profile");
            });
    }

    // OpenID Connect - Microsoft (Task 1.6)
    if (builder.Configuration["Authentication:Microsoft:ClientId"] is { Length: > 0 } msClientId)
    {
        builder.Services.AddAuthentication()
            .AddOpenIdConnect("Microsoft", options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["Authentication:Microsoft:TenantId"]}/v2.0";
                options.ClientId = msClientId;
                options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
                options.ResponseType = "code";
                options.SaveTokens = false;
                options.Scope.Add("email");
                options.Scope.Add("profile");
            });
    }

    builder.Services.AddAuthorization();

    // API Versioning (Task 1.7a)
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    });

    // OpenAPI
    builder.Services.AddOpenApi();

    // Health checks (Task 1.17)
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<Privestio.Infrastructure.Data.PrivestioDbContext>("postgresql");

    // CORS for Blazor WASM
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("BlazorWasm", policy =>
        {
            var webOrigin = builder.Configuration["Web:Origin"] ?? "https://localhost:7001";
            policy.WithOrigins(webOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // Auto-apply migrations on startup (Task 1.18)
    await app.Services.ApplyMigrationsAsync();

    // Global exception handling
    app.UseGlobalExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseHttpsRedirection();
    app.UseCors("BlazorWasm");
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapAuthEndpoints();
    app.MapAccountEndpoints();
    app.MapTransactionEndpoints();

    // Health check endpoints (Task 1.17)
    app.MapHealthChecks("/healthz");
    app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Privestio API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible in tests
public partial class Program { }
