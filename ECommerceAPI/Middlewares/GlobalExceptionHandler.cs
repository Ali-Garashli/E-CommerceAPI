using ECommerceAPI.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
                                                Exception exception,
                                                CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception,
                             "Unhandled exception processing {Method} {Path}",
                             httpContext.Request.Method,
                             httpContext.Request.Path);

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                                   ? "An unexpected error occurred."
                                   : exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception)
        => exception switch
        {
            ProductNotFoundException => (StatusCodes.Status404NotFound, "Product not found"),
            OrderNotFoundException => (StatusCodes.Status404NotFound, "Order not found"),
            UserNotFoundException => (StatusCodes.Status404NotFound, "User not found"),
            UserEmailIsTakenException => (StatusCodes.Status409Conflict, "Email is taken"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Invalid credentials"),
            CategoryNotFoundException => (StatusCodes.Status400BadRequest, "Invalid category"),
            InsufficientStockException => (StatusCodes.Status409Conflict, "Insufficient stock"),
            InvalidOrderStatusTransitionException => (StatusCodes.Status409Conflict, "Invalid status transition"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            IdempotencyKeyInProgressException => (StatusCodes.Status409Conflict, "Request already in progress"),
            IdempotencyKeyMismatchException => (StatusCodes.Status409Conflict, "Idempotency key reused"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };
}

