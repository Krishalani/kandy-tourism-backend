namespace KandyTourism.Api.Domain.Exceptions;

/// <summary>
/// Base type for expected failures. The message is safe to show to API clients; the
/// <see cref="Code"/> lets the frontend react to a specific rule without parsing text.
/// </summary>
public abstract class AppException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string code, string message) : AppException(code, message);

public sealed class ConflictException(string code, string message, IDictionary<string, string[]>? errors = null)
    : AppException(code, message)
{
    public IDictionary<string, string[]>? Errors { get; } = errors;
}

/// <summary>A request that is well-formed but breaks a business rule (HTTP 400).</summary>
public sealed class BusinessRuleException(string code, string message, IDictionary<string, string[]>? errors = null)
    : AppException(code, message)
{
    public IDictionary<string, string[]>? Errors { get; } = errors;
}

public sealed class UnauthorizedException(string code, string message) : AppException(code, message);
