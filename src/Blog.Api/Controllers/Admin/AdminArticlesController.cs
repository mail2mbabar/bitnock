using Blog.Api.Auth;
using Blog.Application.Articles;
using Blog.Application.Common;
using Blog.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers.Admin;

[ApiController]
[Authorize]
[EnableRateLimiting("admin")]
[Route("api/v1/admin/articles")]
public sealed class AdminArticlesController(IArticleService articles) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CanEditArticle)]
    public async Task<ActionResult<Envelope<IReadOnlyList<ArticleListItemDto>>>> List([FromQuery] ArticleQuery query, CancellationToken cancellationToken)
    {
        var result = await articles.ListAsync(query with { PublicOnly = false }, cancellationToken);
        return Ok(Envelope<IReadOnlyList<ArticleListItemDto>>.Ok(result.Items, HttpContext.TraceIdentifier, result.Page, result.PageSize, result.Total));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CanEditArticle)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var article = await articles.GetByIdAsync(id, true, cancellationToken);
        return article is null ? NotFound() : Ok(Envelope<ArticleDetailDto>.Ok(article, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.CanCreateArticle)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Create([FromBody] UpsertArticleRequest request, CancellationToken cancellationToken)
    {
        var created = await articles.CreateAsync(request, ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<ArticleDetailDto>.Ok(created, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.CanEditArticle)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Update(Guid id, [FromBody] UpsertArticleRequest request, CancellationToken cancellationToken)
    {
        var updated = await articles.UpdateAsync(id, request, ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<ArticleDetailDto>.Ok(updated, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.CanPublishArticle)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await articles.DeleteAsync(id, ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<object>.Ok(new { deleted = true }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Permissions.CanPublishArticle)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Publish(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.PublishAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Policy = Permissions.CanPublishArticle)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Unpublish(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.UnpublishAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/schedule")]
    [Authorize(Policy = Permissions.CanPublishArticle)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Schedule(Guid id, [FromBody] ScheduleArticleRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.ScheduleAsync(id, request.ScheduledAt, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = Permissions.CanPublishArticle)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Archive(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.ArchiveAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Permissions.CanEditArticle)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Submit(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.SubmitForReviewAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Permissions.CanApproveArticles)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Approve(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.ApproveAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Permissions.CanApproveArticles)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Reject(Guid id, [FromBody] RejectArticleRequest request, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleDetailDto>.Ok(await articles.RejectAsync(id, request.Reason, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/validate")]
    [Authorize(Policy = Permissions.CanEditArticle)]
    public async Task<ActionResult<Envelope<ArticleValidationResult>>> Validate(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<ArticleValidationResult>.Ok(await articles.ValidateAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/preview")]
    [Authorize(Policy = Permissions.CanEditArticle)]
    public async Task<ActionResult<Envelope<PreviewResult>>> Preview(Guid id, CancellationToken cancellationToken)
        => Ok(Envelope<PreviewResult>.Ok(await articles.CreatePreviewAsync(id, ActorFactory.From(HttpContext), cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}/export.md")]
    [Authorize(Policy = Permissions.CanImportExport)]
    public async Task<IActionResult> ExportMarkdown(Guid id, CancellationToken cancellationToken)
        => Content(await articles.ExportMarkdownAsync(id, cancellationToken), "text/markdown");

    [HttpGet("{id:guid}/export.json")]
    [Authorize(Policy = Permissions.CanImportExport)]
    public async Task<IActionResult> ExportJson(Guid id, CancellationToken cancellationToken)
        => Content(await articles.ExportJsonAsync(id, cancellationToken), "application/json");

    [HttpPost("import")]
    [Authorize(Policy = Permissions.CanImportExport)]
    public async Task<ActionResult<Envelope<ArticleDetailDto>>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var markdown = await reader.ReadToEndAsync(cancellationToken);
        var article = await articles.ImportMarkdownAsync(file.FileName, markdown, ActorFactory.From(HttpContext), cancellationToken);
        return Ok(Envelope<ArticleDetailDto>.Ok(article, HttpContext.TraceIdentifier));
    }
}
