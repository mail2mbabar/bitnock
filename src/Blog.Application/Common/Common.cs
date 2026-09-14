using Blog.Domain.Enums;

namespace Blog.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public sealed record ApiError(string Code, string Message, Dictionary<string, string[]>? Details = null);

public sealed class ActorContext
{
    public required string ActorId { get; init; }
    public required ActorType ActorType { get; init; }
    public required string ActorName { get; init; }
    public Guid? UserId { get; init; }
    public Guid? AgentId { get; init; }
    public Guid? AuthorId { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Scopes { get; init; } = [];
    public string? IpAddress { get; init; }
    public string? RequestId { get; init; }

    public bool IsAdmin => Roles.Contains(Domain.Constants.Roles.Admin, StringComparer.OrdinalIgnoreCase);
    public bool IsEditor => Roles.Contains(Domain.Constants.Roles.Editor, StringComparer.OrdinalIgnoreCase);
    public bool HasScope(string scope) => Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
}

public sealed class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public Dictionary<string, string[]>? Details { get; }

    public AppException(string code, string message, int statusCode = 400, Dictionary<string, string[]>? details = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details;
    }

    public static AppException NotFound(string resource = "Resource")
        => new("NOT_FOUND", $"{resource} was not found.", 404);

    public static AppException Conflict(string code, string message)
        => new(code, message, 409);

    public static AppException Forbidden(string message = "You are not allowed to perform this action.")
        => new("FORBIDDEN", message, 403);

    public static AppException Unauthorized(string message = "Authentication is required.")
        => new("UNAUTHORIZED", message, 401);

    public static AppException Validation(string code, string message, Dictionary<string, string[]>? details = null)
        => new(code, message, 422, details);
}

public sealed record Envelope<T>(
    bool Success,
    T? Data,
    ApiError? Error,
    string RequestId,
    int? Page = null,
    int? PageSize = null,
    int? Total = null)
{
    public static Envelope<T> Ok(T data, string requestId, int? page = null, int? pageSize = null, int? total = null)
        => new(true, data, null, requestId, page, pageSize, total);

    public static Envelope<T> Fail(ApiError error, string requestId)
        => new(false, default, error, requestId);
}

public static class Slugifier
{
    public static string From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lower = value.Trim().ToLowerInvariant();
        var chars = lower.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
