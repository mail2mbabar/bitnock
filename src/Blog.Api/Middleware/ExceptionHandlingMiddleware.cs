using System.Net;
using System.Text.Json;
using Blog.Application.Common;
using Blog.Domain.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;
        var (status, error) = exception switch
        {
            AppException app => (app.StatusCode, new ApiError(app.Code, app.Message, app.Details)),
            DomainException domain => (409, new ApiError(domain.Code, domain.Message)),
            ValidationException validation => (422, new ApiError("VALIDATION_ERROR", "The request is invalid.",
                validation.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()))),
            DbUpdateConcurrencyException => (409, new ApiError("CONCURRENCY_CONFLICT", "The resource was modified by another editor.")),
            UnauthorizedAccessException => (401, new ApiError("UNAUTHORIZED", "Authentication is required.")),
            _ => (500, new ApiError("INTERNAL_ERROR", "An unexpected error occurred."))
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {RequestId}", requestId);
        }
        else
        {
            logger.LogWarning(exception, "Request {RequestId} failed with {Code}", requestId, error.Code);
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        var payload = Envelope<object>.Fail(error, requestId);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonDefaults.Options));
    }
}

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
