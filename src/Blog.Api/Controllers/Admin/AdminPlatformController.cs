using Blog.Api.Auth;
using Blog.Application.Agents;
using Blog.Application.Auth;
using Blog.Application.Common;
using Blog.Application.Media;
using Blog.Application.Taxonomy;
using Blog.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers.Admin;

[ApiController]
[Authorize]
[EnableRateLimiting("admin")]
[Route("api/v1/admin")]
public sealed class AdminPlatformController(
    ITaxonomyService taxonomy,
    IMediaService media,
    IAgentCredentialService agents,
    IAuditService audit,
    IAnalyticsService analytics,
    ISettingsService settings) : ControllerBase
{
    [HttpGet("dashboard")]
    [Authorize(Policy = Permissions.CanViewAnalytics)]
    public async Task<ActionResult<Envelope<AnalyticsSummaryDto>>> Dashboard(CancellationToken cancellationToken)
        => Ok(Envelope<AnalyticsSummaryDto>.Ok(await analytics.GetDashboardAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("categories")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<IReadOnlyList<CategoryDto>>>> Categories(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<CategoryDto>>.Ok(await taxonomy.ListCategoriesAsync(false, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("categories")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<CategoryDto>>> CreateCategory([FromBody] UpsertCategoryRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<CategoryDto>.Ok(await taxonomy.CreateCategoryAsync(request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<CategoryDto>>> UpdateCategory(Guid id, [FromBody] UpsertCategoryRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<CategoryDto>.Ok(await taxonomy.UpdateCategoryAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await taxonomy.DeleteCategoryAsync(id, cancellationToken);
        return Ok(Envelope<object>.Ok(new { deleted = true }, HttpContext.TraceIdentifier));
    }

    [HttpGet("tags")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<IReadOnlyList<TagDto>>>> Tags([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await taxonomy.ListTagsAsync(page, pageSize, search, cancellationToken);
        return Ok(Envelope<IReadOnlyList<TagDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpPost("tags")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<TagDto>>> CreateTag([FromBody] UpsertTagRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<TagDto>.Ok(await taxonomy.CreateTagAsync(request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPut("tags/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<TagDto>>> UpdateTag(Guid id, [FromBody] UpsertTagRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<TagDto>.Ok(await taxonomy.UpdateTagAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpDelete("tags/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
    {
        await taxonomy.DeleteTagAsync(id, cancellationToken);
        return Ok(Envelope<object>.Ok(new { deleted = true }, HttpContext.TraceIdentifier));
    }

    [HttpGet("series")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<IReadOnlyList<SeriesDto>>>> Series(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<SeriesDto>>.Ok(await taxonomy.ListSeriesAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("series")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<SeriesDto>>> CreateSeries([FromBody] UpsertSeriesRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<SeriesDto>.Ok(await taxonomy.CreateSeriesAsync(request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPut("series/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<ActionResult<Envelope<SeriesDto>>> UpdateSeries(Guid id, [FromBody] UpsertSeriesRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<SeriesDto>.Ok(await taxonomy.UpdateSeriesAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpDelete("series/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageTaxonomy)]
    public async Task<IActionResult> DeleteSeries(Guid id, CancellationToken cancellationToken)
    {
        await taxonomy.DeleteSeriesAsync(id, cancellationToken);
        return Ok(Envelope<object>.Ok(new { deleted = true }, HttpContext.TraceIdentifier));
    }

    [HttpGet("authors")]
    [Authorize(Policy = Permissions.CanManageUsers)]
    public async Task<ActionResult<Envelope<IReadOnlyList<AuthorDto>>>> Authors(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<AuthorDto>>.Ok(await taxonomy.ListAuthorsAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("authors")]
    [Authorize(Policy = Permissions.CanManageUsers)]
    public async Task<ActionResult<Envelope<AuthorDto>>> CreateAuthor([FromBody] UpsertAuthorRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<AuthorDto>.Ok(await taxonomy.CreateAuthorAsync(request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPut("authors/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageUsers)]
    public async Task<ActionResult<Envelope<AuthorDto>>> UpdateAuthor(Guid id, [FromBody] UpsertAuthorRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<AuthorDto>.Ok(await taxonomy.UpdateAuthorAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("media")]
    [Authorize(Policy = Permissions.CanManageMedia)]
    public async Task<ActionResult<Envelope<IReadOnlyList<MediaAssetDto>>>> Media([FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await media.ListAsync(page, pageSize, search, cancellationToken);
        return Ok(Envelope<IReadOnlyList<MediaAssetDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpPost("media")]
    [Authorize(Policy = Permissions.CanManageMedia)]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<Envelope<MediaAssetDto>>> Upload(IFormFile file, [FromForm] string? altText, [FromForm] string? title, [FromForm] string? description, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var asset = await media.UploadAsync(new MediaUploadCommand(stream, file.FileName, file.ContentType, altText, title, description), ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<MediaAssetDto>.Ok(asset, HttpContext.TraceIdentifier));
    }

    [HttpPut("media/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageMedia)]
    public async Task<ActionResult<Envelope<MediaAssetDto>>> UpdateMedia(Guid id, [FromBody] UpdateMediaRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<MediaAssetDto>.Ok(await media.UpdateAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpDelete("media/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageMedia)]
    public async Task<IActionResult> DeleteMedia(Guid id, CancellationToken cancellationToken)
    {
        await media.DeleteAsync(id, ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<object>.Ok(new { deleted = true }, HttpContext.TraceIdentifier));
    }

    [HttpGet("agents")]
    [Authorize(Policy = Permissions.CanManageAgentKeys)]
    public async Task<ActionResult<Envelope<IReadOnlyList<AgentCredentialDto>>>> Agents([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await agents.ListAsync(page, pageSize, cancellationToken);
        return Ok(Envelope<IReadOnlyList<AgentCredentialDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpPost("agents")]
    [Authorize(Policy = Permissions.CanManageAgentKeys)]
    public async Task<ActionResult<Envelope<CreatedAgentCredentialDto>>> CreateAgent([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var actor = ActorFactory.From(HttpContext);
        var created = await agents.CreateAsync(request, actor.UserId ?? Guid.Empty, cancellationToken);
        return Ok(Envelope<CreatedAgentCredentialDto>.Ok(created, HttpContext.TraceIdentifier));
    }

    [HttpPut("agents/{id:guid}")]
    [Authorize(Policy = Permissions.CanManageAgentKeys)]
    public async Task<ActionResult<Envelope<AgentCredentialDto>>> UpdateAgent(Guid id, [FromBody] UpdateAgentRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<AgentCredentialDto>.Ok(await agents.UpdateAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("agents/{id:guid}/revoke")]
    [Authorize(Policy = Permissions.CanManageAgentKeys)]
    public async Task<IActionResult> RevokeAgent(Guid id, CancellationToken cancellationToken)
    {
        await agents.RevokeAsync(id, cancellationToken);
        return Ok(Envelope<object>.Ok(new { revoked = true }, HttpContext.TraceIdentifier));
    }

    [HttpPost("agents/{id:guid}/rotate")]
    [Authorize(Policy = Permissions.CanManageAgentKeys)]
    public async Task<ActionResult<Envelope<CreatedAgentCredentialDto>>> RotateAgent(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<CreatedAgentCredentialDto>.Ok(await agents.RotateAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("audit-logs")]
    [Authorize(Policy = Permissions.CanViewAuditLogs)]
    public async Task<ActionResult<Envelope<IReadOnlyList<AuditLogDto>>>> Audit([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? actor = null, [FromQuery] string? action = null, [FromQuery] string? resourceType = null, CancellationToken cancellationToken = default)
    {
        var result = await audit.ListAsync(page, pageSize, actor, action, resourceType, cancellationToken);
        return Ok(Envelope<IReadOnlyList<AuditLogDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpGet("publishing-rules")]
    [Authorize(Policy = Permissions.CanManageSettings)]
    public async Task<ActionResult<Envelope<Blog.Application.Articles.PublishingRulesDto>>> Rules(CancellationToken cancellationToken)
        => Ok(Envelope<Blog.Application.Articles.PublishingRulesDto>.Ok(await settings.GetPublishingRulesAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpPut("publishing-mode")]
    [Authorize(Policy = Permissions.CanManageSettings)]
    public async Task<IActionResult> Mode([FromBody] PublishingModeRequest request, CancellationToken cancellationToken)
    {
        await settings.UpdatePublishingModeAsync(request.Mode, cancellationToken);
        return Ok(Envelope<object>.Ok(new { mode = request.Mode }, HttpContext.TraceIdentifier));
    }
}

public sealed record PublishingModeRequest(string Mode);
