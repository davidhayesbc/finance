using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Privestio.Application.Services;

namespace Privestio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(Behaviors.ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<NotificationService>();
        services.AddScoped<NetWorthForecastingService>();
        services.AddScoped<AmortizationScheduleService>();
        services.AddScoped<ResourcePermissionService>();
        services.AddScoped<IDataExportService, DataExportService>();
        services.AddScoped<IConflictResolutionService, ConflictResolutionService>();

        return services;
    }
}
