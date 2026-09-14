using Blog.Api.Auth;
using Blog.Application.Auth;
using Blog.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<Envelope<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request.Email, request.Password, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(Envelope<AuthResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<Envelope<AuthResponse>>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.RefreshAsync(request.RefreshToken, cancellationToken);
        return Ok(Envelope<AuthResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<Envelope<object>>> Logout([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(request.RefreshToken, cancellationToken);
        return Ok(Envelope<object>.Ok(new { loggedOut = true }, HttpContext.TraceIdentifier));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<Envelope<UserProfileDto>>> Me(CancellationToken cancellationToken)
    {
        var actor = ActorFactory.From(HttpContext);
        if (actor.UserId is null)
        {
            return Unauthorized();
        }

        var profile = await auth.GetProfileAsync(actor.UserId.Value, cancellationToken);
        return profile is null
            ? NotFound()
            : Ok(Envelope<UserProfileDto>.Ok(profile, HttpContext.TraceIdentifier));
    }
}
