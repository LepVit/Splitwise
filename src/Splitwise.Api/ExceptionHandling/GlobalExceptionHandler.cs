using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Core.Exceptions;

namespace Splitwise.Api.ExceptionHandling;

/// <summary>Єдине місце, де винятки перетворюються на HTTP-відповідь у форматі ProblemDetails.</summary>
public class GlobalExceptionHandler(IProblemDetailsService problems, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Failure"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            ConflictException => (StatusCodes.Status409Conflict, "Business Conflict"),
            DomainException => (StatusCodes.Status400BadRequest, "Business Rule Violation"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", http.Request.Method, http.Request.Path);
        else
            logger.LogWarning("Handled {ExceptionType} ({Status}): {Message}", exception.GetType().Name, status, exception.Message);

        http.Response.StatusCode = status;

        ProblemDetails body = exception is ValidationException validation
            ? new HttpValidationProblemDetails(validation.Errors)
            {
                Status = status,
                Title = title,
                Detail = exception.Message
            }
            : new ProblemDetails
            {
                Status = status,
                Title = title,
                // Для 500 клієнту не віддаємо текст винятку й stack trace
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "An internal error occurred."
                    : exception.Message
            };

        body.Instance = http.Request.Path;

        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = body
        });
    }
}
