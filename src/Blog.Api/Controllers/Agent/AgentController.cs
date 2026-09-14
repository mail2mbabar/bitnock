using Blog.Api.Auth;
using Blog.Application.Articles;
using Blog.Application.Auth;
using Blog.Application.Common;
using Blog.Application.Media;
using Blog.Application.Taxonomy;
using Blog.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers.Agent;

[ApiController]
[Authorize(Policy = "AgentOnly")]
[EnableRateLimiting("agent")]
[Route("api/v1/agent")]
public sealed class AgentController(
    IArticleService articles,
    ITaxonomyService taxonomy,
    IMediaService media,
    ISearchService search,
    ISettingsService settings,
    IAnalyticsService analytics) : ControllerBase
{
    [HttpGet("publishing-rules")]
    [RequiresScope(AgentScopes.RulesRead)]
    public async Task<ActionResult<Envelope<PublishingRulesDto>>> Rules(CancellationToken cancellationToken)
        => Ok(Envelope<PublishingRulesDto>.Ok(await settings.GetPublishingRulesAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("articles")]
    [RequiresScope(AgentScopes.ArticlesRead)]
    public async Task<ActionResult<Envelope<IReadOnlyList<ArticleListItemDto>>>> List([FromQuery] ArticleQuery query, CancellationToken cancellationToken)
    {
        var result = await articles.ListAsync(query with { PublicOnly = false }, cancellationToken);
        return Ok(Envelope<IReadOnlyList<ArticleListItemDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpGet("articles/search")]
    [RequiresScope(AgentScopes.ArticlesRead)]
    public async Task<ActionResult<Envelope<IReadOnlyList<ArticleListItemDto>>>> Search([FromQuery] string q, [FromQuery] ArticleQuery query, CancellationToken cancellationToken)
    {
        var result = await search.SearchAsync(q ?? string.Empty, query, cancellationToken);
        return Ok(Envelope<IReadOnlyList<ArticleListItemDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpGet("articles/{id:guid}")]
    [RequiresScope(AgentScopes.ArticlesRead)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var article = await articles.GetByIdAsync(id, true, cancellationToken);
        return article is null
            ? NotFound(Envelope<ArticleDetailDto>.Fail(new ApiError("ARTICLE_NOT_FOUND", "Article was not found."), HttpContext.TraceIdentifier))
            : Ok(Envelope<ArticleDetailDto>.Ok(article, HttpContext.TraceIdentifier));
    }

    [HttpPost("articles")]
    [RequiresScope(AgentScopes.ArticlesCreate)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Create([FromBody] UpsertArticleRequest request, CancellationToken cancellationToken)
    {
        var created = await articles.CreateAsync(request with { IsAiGenerated = true }, ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<ArticleDetailDto>.Ok(created, HttpContext.TraceIdentifier));
    }

    [HttpPut("articles/{id:guid}")]
    [RequiresScope(AgentScopes.ArticlesUpdate)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Update(Guid id, [FromBody] UpsertArticleRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.UpdateAsync(id, request, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpDelete("articles/{id:guid}")]
    [RequiresScope(AgentScopes.ArticlesDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await articles.DeleteAsync(id, ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<object>.Ok(new { deleted = true }, HttpContext.TraceIdentifier));
    }

    [HttpPost("articles/{id:guid}/publish")]
    [RequiresScope(AgentScopes.ArticlesPublish)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Publish(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.PublishAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("articles/{id:guid}/unpublish")]
    [RequiresScope(AgentScopes.ArticlesPublish)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Unpublish(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.UnpublishAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("articles/{id:guid}/schedule")]
    [RequiresScope(AgentScopes.ArticlesSchedule)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Schedule(Guid id, [FromBody] ScheduleArticleRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.ScheduleAsync(id, request.ScheduledAt, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("articles/{id:guid}/preview")]
    [RequiresScope(AgentScopes.ArticlesRead)]
    public async Task<ActionResult<Envelope<PreviewResult>>> Preview(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<PreviewResult>.Ok(await articles.CreatePreviewAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("articles/{id:guid}/validate")]
    [RequiresScope(AgentScopes.ArticlesValidate)]
    public async Task<ActionResult<Envelope<ArticleValidationResult>>> Validate(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleValidationResult>.Ok(await articles.ValidateAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("articles/{id:guid}/duplicates")]
    [RequiresScope(AgentScopes.ArticlesRead)]
    public async Task<ActionResult<Envelope<DuplicateCheckResult>>> Duplicates(Guid id, CancellationToken cancellationToken)
    {
        var article = await articles.GetByIdAsync(id, true, cancellationToken)
                      ?? throw AppException.NotFound("Article");
        var result = await articles.CheckDuplicatesAsync(id, article.Title, article.Slug, article.Content, cancellationToken);
        return Ok(Envelope<DuplicateCheckResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("articles/{id:guid}/related")]
    [RequiresScope(AgentScopes.ArticlesRead)]
    public async Task<ActionResult<Envelope<IReadOnlyList<ArticleListItemDto>>>> Related(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<ArticleListItemDto>>.Ok(await articles.GetRelatedAsync(id, 5, cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("categories")]
    [RequiresScope(AgentScopes.CategoriesRead)]
    public async Task<ActionResult<Envelope<IReadOnlyList<CategoryDto>>>> Categories(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<CategoryDto>>.Ok(await taxonomy.ListCategoriesAsync(true, cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("tags")]
    [RequiresScope(AgentScopes.TagsRead)]
    public async Task<ActionResult<Envelope<IReadOnlyList<TagDto>>>> Tags(CancellationToken cancellationToken)
    {
        var result = await taxonomy.ListTagsAsync(1, 200, null, cancellationToken);
        return Ok(Envelope<IReadOnlyList<TagDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpGet("series")]
    [RequiresScope(AgentScopes.SeriesRead)]
    public async Task<ActionResult<Envelope<IReadOnlyList<SeriesDto>>>> Series(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<SeriesDto>>.Ok(await taxonomy.ListSeriesAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("media")]
    [RequiresScope(AgentScopes.MediaUpload)]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<Envelope<MediaAssetDto>>> Upload(IFormFile file, [FromForm] string? altText, [FromForm] string? title, [FromForm] string? description, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var asset = await media.UploadAsync(new MediaUploadCommand(stream, file.FileName, file.ContentType, altText, title, description), ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<MediaAssetDto>.Ok(asset, HttpContext.TraceIdentifier));
    }

    [HttpGet("media")]
    [RequiresScope(AgentScopes.MediaRead)]
    public async Task<ActionResult<Envelope<IReadOnlyList<MediaAssetDto>>>> Media([FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await media.ListAsync(page, pageSize, search, cancellationToken);
        return Ok(Envelope<IReadOnlyList<MediaAssetDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpGet("analytics")]
    [RequiresScope(AgentScopes.AnalyticsRead)]
    public async Task<ActionResult<Envelope<AnalyticsSummaryDto>>> Analytics(CancellationToken cancellationToken)
        => Ok(Envelope<AnalyticsSummaryDto>.Ok(await analytics.GetDashboardAsync(cancellationToken), HttpContext.TraceIdentifier));
}
