using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Privestio.Application.Queries.GetCashFlowForecast;
using Privestio.Application.Queries.GetNotifications;
using Privestio.Application.Services;

namespace Privestio.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group
            .MapGet("/", GetNotificationsAsync)
            .WithName("GetNotifications")
            .WithSummary("Get notifications for the current user");

        group
            .MapPost("/{id:guid}/read", MarkAsReadAsync)
            .WithName("MarkNotificationAsRead")
            .WithSummary("Mark a notification as read");

        group
            .MapPost("/read-all", MarkAllAsReadAsync)
            .WithName("MarkAllNotificationsAsRead")
            .WithSummary("Mark all notifications as read");

        group
            .MapPost("/check-alerts", CheckAlertsAsync)
            .WithName("CheckAlerts")
            .WithSummary(
                "Run alert checks for minimum balances, budget overages, and sinking funds"
            );

        // Cash flow forecast is related to budgeting/notifications
        var forecastGroup = app.MapGroup("/api/v1/forecast")
            .WithTags("Forecasting")
            .RequireAuthorization();

        forecastGroup
            .MapGet("/cash-flow", GetCashFlowForecastAsync)
            .WithName("GetCashFlowForecast")
            .WithSummary("Get cash flow forecast for the next N months");

        return app;
    }

    private static async Task<IResult> GetNotificationsAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] bool? includeRead,
        [FromQuery] int? limit,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetNotificationsQuery(userId.Value, includeRead ?? false, limit ?? 50),
            cancellationToken
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> MarkAsReadAsync(
        Guid id,
        IMediator mediator,
        ClaimsPrincipal user,
        Privestio.Application.Interfaces.IUnitOfWork unitOfWork,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var notification = await unitOfWork.Notifications.GetByIdAsync(id, cancellationToken);
        if (notification is null || notification.UserId != userId.Value)
            return Results.NotFound();

        await unitOfWork.Notifications.MarkAsReadAsync(id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllAsReadAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        Privestio.Application.Interfaces.IUnitOfWork unitOfWork,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        await unitOfWork.Notifications.MarkAllAsReadAsync(userId.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> CheckAlertsAsync(
        [FromQuery] decimal? minimumBalance,
        [FromQuery] int? year,
        [FromQuery] int? month,
        ClaimsPrincipal user,
        NotificationService notificationService,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var now = DateTime.UtcNow;
        var checkYear = year ?? now.Year;
        var checkMonth = month ?? now.Month;

        await notificationService.CheckMinimumBalanceAlerts(
            userId.Value,
            minimumBalance ?? 500m,
            cancellationToken
        );
        await notificationService.CheckBudgetOverageAlerts(
            userId.Value,
            checkYear,
            checkMonth,
            cancellationToken
        );
        await notificationService.CheckSinkingFundAlerts(userId.Value, cancellationToken);

        return Results.Ok(new { Message = "Alert checks completed." });
    }

    private static async Task<IResult> GetCashFlowForecastAsync(
        IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] int? months,
        CancellationToken cancellationToken
    )
    {
        var userId = EndpointHelpers.GetUserId(user);
        if (userId is null)
            return Results.Unauthorized();

        var result = await mediator.Send(
            new GetCashFlowForecastQuery(userId.Value, months ?? 6),
            cancellationToken
        );
        return Results.Ok(result);
    }
}
