using System.Security.Cryptography;
using System.Text;
using Blog.Application.Common;
using Blog.Domain.Entities;
using Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Middleware;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            && !HttpMethods.IsPut(context.Request.Method)
            && !HttpMethods.IsPatch(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (!context.Request.Path.StartsWithSegments("/api/v1/agent"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
        {
            await next(context);
            return;
        }

        var key = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 120)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(Envelope<object>.Fail(
                new ApiError("INVALID_IDEMPOTENCY_KEY", "Idempotency-Key is invalid."),
                context.TraceIdentifier));
            return;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{context.Request.Method}:{context.Request.Path}:{body}")));

        var agentIdValue = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(agentIdValue, out var agentId))
        {
            await next(context);
            return;
        }

        var db = context.RequestServices.GetRequiredService<BlogDbContext>();
        var existing = await db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.AgentId == agentId && r.IdempotencyKey == key, context.RequestAborted);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(Envelope<object>.Fail(
                    new ApiError("IDEMPOTENCY_KEY_REUSE", "Idempotency-Key was reused with a different payload."),
                    context.TraceIdentifier));
                return;
            }

            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }

        var original = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        await next(context);
        buffer.Position = 0;
        var responseBody = await new StreamReader(buffer).ReadToEndAsync();
        buffer.Position = 0;
        await buffer.CopyToAsync(original);
        context.Response.Body = original;

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            db.IdempotencyRecords.Add(IdempotencyRecord.Create(
                key, agentId, requestHash, context.Request.Method, context.Request.Path,
                context.Response.StatusCode, responseBody, DateTimeOffset.UtcNow, TimeSpan.FromHours(24)));
            await db.SaveChangesAsync(context.RequestAborted);
        }
    }
}
