using Blog.Application.Common;
using Blog.Domain.Constants;
using Blog.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Blog.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresScopeAttribute(string scope) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.IsInRole(Roles.Admin))
        {
            await next();
            return;
        }

        var scopes = user.FindAll("scope").Select(c => c.Value).ToArray();
        if (!scopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(Envelope<object>.Fail(
                new ApiError("INSUFFICIENT_SCOPE", $"This operation requires the '{scope}' scope."),
                context.HttpContext.TraceIdentifier))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}

public static class ActorFactory
{
    public static ActorContext From(HttpContext http)
    {
        var user = http.User;
        var isAgent = user.FindFirst("actor_type")?.Value == "Agent"
                      || user.IsInRole(Roles.Agent);
        var id = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var name = user.Identity?.Name ?? "anonymous";
        var author = user.FindFirst("author_id")?.Value;
        return new ActorContext
        {
            ActorId = id,
            ActorType = isAgent ? ActorType.Agent : user.Identity?.IsAuthenticated == true ? ActorType.User : ActorType.System,
            ActorName = name,
            UserId = isAgent ? null : Guid.TryParse(id, out var userId) ? userId : null,
            AgentId = isAgent && Guid.TryParse(id, out var agentId) ? agentId : null,
            AuthorId = Guid.TryParse(author, out var authorId) ? authorId : null,
            Roles = user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray(),
            Scopes = user.FindAll("scope").Select(c => c.Value).ToArray(),
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            RequestId = http.TraceIdentifier
        };
    }
}
