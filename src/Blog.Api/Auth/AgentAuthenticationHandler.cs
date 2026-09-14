using System.Security.Claims;
using System.Text.Encodings.Web;
using Blog.Application.Agents;
using Blog.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Blog.Api.Auth;

public sealed class AgentAuthenticationOptions : AuthenticationSchemeOptions
{
}

public sealed class AgentAuthenticationHandler(
    IOptionsMonitor<AgentAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AgentAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "Agent";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var agents = Context.RequestServices.GetRequiredService<IAgentCredentialService>();
        var key = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key)
            && Request.Headers.Authorization.FirstOrDefault() is { } header
            && header.StartsWith("Bearer nxa_", StringComparison.Ordinal))
        {
            key = header["Bearer ".Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("nxa_", StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var agent = await agents.AuthenticateAsync(key, Context.RequestAborted);
        if (agent is null || agent.Status != AgentCredentialStatus.Active)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, agent.Id.ToString()),
            new(ClaimTypes.Name, agent.Name),
            new("actor_type", "Agent"),
            new(ClaimTypes.Role, Domain.Constants.Roles.Agent)
        };
        claims.AddRange(agent.Scopes.Select(scope => new Claim("scope", scope)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
