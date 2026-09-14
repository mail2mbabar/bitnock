using Blog.Application.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("media")]
public sealed class MediaDownloadController(IStorageService storage) : ControllerBase
{
    [HttpGet("{**key}")]
    [HttpHead("{**key}")]
    [ResponseCache(Duration = 86400)]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        if (key.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(key))
        {
            return BadRequest();
        }

        try
        {
            var stream = await storage.OpenReadAsync(key, cancellationToken);
            var contentType = key switch
            {
                _ when key.EndsWith(".png", StringComparison.OrdinalIgnoreCase) => "image/png",
                _ when key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || key.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) => "image/jpeg",
                _ when key.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) => "image/webp",
                _ when key.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) => "image/gif",
                _ when key.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) => "image/svg+xml",
                _ => "application/octet-stream"
            };
            return File(stream, contentType);
        }
        catch (IOException)
        {
            return NotFound();
        }
    }
}
