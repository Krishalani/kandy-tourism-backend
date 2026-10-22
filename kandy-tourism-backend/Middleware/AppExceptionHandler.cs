using KandyTourism.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KandyTourism.Api.Middleware;

/// <summary>
/// Converts exceptions into RFC 7807 problem details. Expected failures keep their safe message and
/// a machine-readable <c>code</c>; anything else becomes a generic 500 without internal details.
/// </summary>
public sealed class AppExceptionHandler(IProblemDetailsService problemDetails, ILogger<AppExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, code, detail, errors) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Code, e.Message, null),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Code, e.Message, e.Errors),
            BusinessRuleException e => (StatusCodes.Status400BadRequest, "Request not allowed", e.Code, e.Message, e.Errors),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, "Unauthorized", e.Code, e.Message, null),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict", "concurrency-conflict",
                "The data changed while you were saving. Please reload and try again.", null),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflict", "data-conflict",
                "The change conflicts with existing data. Please reload and try again.", (IDictionary<string, string[]>?)null),
            OperationCanceledException when context.RequestAborted.IsCancellationRequested =>
                (499, "Request cancelled", "request-cancelled", "The request was cancelled.", null),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "server-error",
                "Something went wrong on our side. Please try again later.", null),
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else if (exception is DbUpdateException)
        {
            logger.LogWarning(exception, "Database update conflict for {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
