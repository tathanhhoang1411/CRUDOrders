using System.Net;
using System.Text.Json;
using CRUDOrders.Application.DTOs;
using FluentValidation;

namespace CRUDOrders.Presentation.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        Result<object> response;

        switch (exception)
        {
            case OperationCanceledException:
                // Request bi huy — khong can log loi
                context.Response.StatusCode = 499; // Client Closed Request
                _logger.LogInformation("Request {Path} was cancelled by client", context.Request.Path);
                return;

            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errors = string.Join(", ", validationEx.Errors.Select(e => e.ErrorMessage));
                // Log chi tiet loi validation trong log
                _logger.LogWarning(validationEx, "[Validation] {Path} | Errors: {Errors}",
                    context.Request.Path, errors);
                response = Result<object>.Error("VALIDATION_ERROR", errors);
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                _logger.LogWarning(exception, "[Unauthorized] {Path}", context.Request.Path);
                response = Result<object>.Error("UNAUTHORIZED", "Khong co quyen truy cap");
                break;

            case KeyNotFoundException keyNotFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                _logger.LogWarning(exception, "[NotFound] {Path} | {Message}",
                    context.Request.Path, keyNotFoundEx.Message);
                response = Result<object>.Error("NOT_FOUND", "Khong tim thay du lieu yeu cau");
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                // Ghi FULL stack trace vao log de debug
                _logger.LogError(exception,
                    "[UnhandledException] {Method} {Path} | {ExceptionType}: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    exception.GetType().Name,
                    exception.Message);
                // Tra ve message tong quat cho client — khong lo thong tin noi bo
                response = Result<object>.Error("INTERNAL_SERVER_ERROR", "Da xay ra loi. Vui long thu lai sau hoac lien he ho tro.");
                break;
        }

        await context.Response.WriteAsJsonAsync(response);
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}
