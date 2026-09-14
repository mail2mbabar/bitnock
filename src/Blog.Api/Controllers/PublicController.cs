using Blog.Application.Articles;
using Blog.Application.Auth;
using Blog.Application.Common;
using Blog.Application.Taxonomy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public")]
[Route("api/v1/public")]
public sealed class PublicController(
    IArticleService articles,
    ITaxonomyService taxonomy,
    ISearchService search,
    INewsletterService newsletter,
    ISettingsService settings,
    IAnalyticsService analytics,
    IMarkdownService markdown) : ControllerBase
{
    [HttpGet("settings")]
    [OutputCache(Duration = 60)]
    public async Task<ActionResult<Envelope<SiteSettingsDto>>> Settings(CancellationToken cancellationToken)
        => Ok(Envelope<SiteSettingsDto>.Ok(await settings.GetPublicAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("articles")]
    [OutputCache(Duration = 30)]
    public async Task<ActionResult<Envelope<IReadOnlyList<ArticleListItemDto>>>> Articles([FromQuery] ArticleQuery query, CancellationToken cancellationToken)
    {
        var result = await articles.ListAsync(query with { PublicOnly = true }, cancellationToken);
        return Ok(Envelope<IReadOnlyList<ArticleListItemDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpGet("articles/{slug}")]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Article(string slug, CancellationToken cancellationToken)
    {
        var article = await articles.GetBySlugAsync(slug, true, cancellationToken);
        return article is null
            ? NotFound(Envelope<ArticleDetailDto>.Fail(new ApiError("ARTICLE_NOT_FOUND", "Article was not found."), HttpContext.TraceIdentifier))
            : Ok(Envelope<ArticleDetailDto>.Ok(article, HttpContext.TraceIdentifier));
    }

    [HttpGet("articles/{slug}/html")]
    public async Task<ActionResult<Envelope<object>>> ArticleHtml(string slug, CancellationToken cancellationToken)
    {
        var article = await articles.GetBySlugAsync(slug, true, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        return Ok(Envelope<object>.Ok(new { html = markdown.ToSafeHtml(article.Content), tableOfContents = article.TableOfContents }, HttpContext.TraceIdentifier));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<Envelope<IReadOnlyList<CategoryDto>>>> Categories(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<CategoryDto>>.Ok(await taxonomy.ListCategoriesAsync(true, cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("categories/{slug}")]
    public async Task<ActionResult<Envelope<CategoryDto>>> Category(string slug, CancellationToken cancellationToken)
    {
        var item = await taxonomy.GetCategoryBySlugAsync(slug, cancellationToken);
        return item is null ? NotFound() : Ok(Envelope<CategoryDto>.Ok(item, HttpContext.TraceIdentifier));
    }

    [HttpGet("tags")]
    public async Task<ActionResult<Envelope<IReadOnlyList<TagDto>>>> Tags([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await taxonomy.ListTagsAsync(page, pageSize, null, cancellationToken);
        return Ok(Envelope<IReadOnlyList<TagDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpGet("tags/{slug}")]
    public async Task<ActionResult<Envelope<TagDto>>> Tag(string slug, CancellationToken cancellationToken)
    {
        var item = await taxonomy.GetTagBySlugAsync(slug, cancellationToken);
        return item is null ? NotFound() : Ok(Envelope<TagDto>.Ok(item, HttpContext.TraceIdentifier));
    }

    [HttpGet("series")]
    public async Task<ActionResult<Envelope<IReadOnlyList<SeriesDto>>>> Series(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<SeriesDto>>.Ok(await taxonomy.ListSeriesAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("series/{slug}")]
    public async Task<ActionResult<Envelope<SeriesDetailDto>>> SeriesDetail(string slug, CancellationToken cancellationToken)
    {
        var item = await taxonomy.GetSeriesBySlugAsync(slug, cancellationToken);
        return item is null ? NotFound() : Ok(Envelope<SeriesDetailDto>.Ok(item, HttpContext.TraceIdentifier));
    }

    [HttpGet("authors")]
    public async Task<ActionResult<Envelope<IReadOnlyList<AuthorDto>>>> Authors(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<AuthorDto>>.Ok(await taxonomy.ListAuthorsAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("authors/{slug}")]
    public async Task<ActionResult<Envelope<AuthorDto>>> Author(string slug, CancellationToken cancellationToken)
    {
        var item = await taxonomy.GetAuthorBySlugAsync(slug, cancellationToken);
        return item is null ? NotFound() : Ok(Envelope<AuthorDto>.Ok(item, HttpContext.TraceIdentifier));
    }

    [HttpGet("technologies")]
    public async Task<ActionResult<Envelope<IReadOnlyList<TechnologyDto>>>> Technologies(CancellationToken cancellationToken)
        => Ok(Envelope<IReadOnlyList<TechnologyDto>>.Ok(await taxonomy.ListTechnologiesAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("search")]
    public async Task<ActionResult<Envelope<IReadOnlyList<ArticleListItemDto>>>> Search([FromQuery] string q, [FromQuery] ArticleQuery query, CancellationToken cancellationToken)
    {
        var result = await search.SearchAsync(q ?? string.Empty, query with { PublicOnly = true }, cancellationToken);
        return Ok(Envelope<IReadOnlyList<ArticleListItemDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpPost("newsletter")]
    public async Task<ActionResult<Envelope<object>>> Newsletter([FromBody] NewsletterSubscribeRequest request, CancellationToken cancellationToken)
    {
        await newsletter.SubscribeAsync(request.Email, cancellationToken);
        return Ok(Envelope<object>.Ok(new { subscribed = true }, HttpContext.TraceIdentifier));
    }

    [HttpPost("newsletter/confirm")]
    public async Task<ActionResult<Envelope<object>>> Confirm([FromBody] NewsletterConfirmRequest request, CancellationToken cancellationToken)
    {
        await newsletter.ConfirmAsync(request.Token, cancellationToken);
        return Ok(Envelope<object>.Ok(new { confirmed = true }, HttpContext.TraceIdentifier));
    }

    [HttpPost("analytics/pageview")]
    public async Task<ActionResult<Envelope<object>>> PageView([FromBody] TrackPageViewRequest request, CancellationToken cancellationToken)
    {
        await analytics.TrackPageViewAsync(request, cancellationToken);
        return Ok(Envelope<object>.Ok(new { tracked = true }, HttpContext.TraceIdentifier));
    }

    [HttpGet("preview/{id:guid}")]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Preview(Guid id, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var article = await articles.GetByPreviewTokenAsync(token, cancellationToken);
        if (article is null || article.Id != id)
        {
            return NotFound(Envelope<ArticleDetailDto>.Fail(new ApiError("PREVIEW_NOT_FOUND", "Preview is invalid or expired."), HttpContext.TraceIdentifier));
        }

        return Ok(Envelope<ArticleDetailDto>.Ok(article, HttpContext.TraceIdentifier));
    }
}
