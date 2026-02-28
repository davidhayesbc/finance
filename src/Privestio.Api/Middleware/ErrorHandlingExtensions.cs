using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace Privestio.Api.Middleware;

/// <summary>
/// Global exception handling middleware that converts unhandled exceptions to ProblemDetails responses.
/// </summary>
public static class ErrorHandlingExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (exceptionFeature is null) return;

                var exception = exceptionFeature.Error;
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

                context.Response.ContentType = "application/problem+json";

                var (statusCode, title, detail) = exception switch
                {
                    FluentValidation.ValidationException vex => (
                        StatusCodes.Status400BadRequest,
                        "Validation failed",
                        string.Join("; ", vex.Errors.Select(e => e.ErrorMessage))),
                    UnauthorizedAccessException => (
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized",
                        "You are not authorized to perform this action."),
                    KeyNotFoundException => (
                        StatusCodes.Status404NotFound,
                        "Not found",
                        exception.Message),
                    _ => (
                        StatusCodes.Status500InternalServerError,
                        "An unexpected error occurred",
                        app.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                            ? exception.Message
                            : "An internal server error occurred."),
                };

                context.Response.StatusCode = statusCode;

                var problem = new
                {
                    type = $"https://httpstatuses.com/{statusCode}",
                    title,
                    status = statusCode,
                    detail,
                    traceId = context.TraceIdentifier,
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            });
        });

        return app;
    }
}
