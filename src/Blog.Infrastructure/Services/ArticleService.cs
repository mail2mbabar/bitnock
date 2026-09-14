using System.Text.Json;
using Blog.Application.Articles;
using Blog.Application.Auth;
using Blog.Application.Agents;
using Blog.Application.Common;
using Blog.Application.Options;
using Blog.Domain.Constants;
using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Infrastructure.Mapping;
using Blog.Infrastructure.Persistence;
using Blog.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Blog.Infrastructure.Services;

public sealed class ArticleService : IArticleService
{
    private readonly BlogDbContext _db;
    private readonly IArticleValidationService _validation;
    private readonly IDuplicateDetectionService _duplicates;
    private readonly IRecommendationService _recommendations;
    private readonly IMarkdownService _markdown;
    private readonly IAuditService _audit;
    private readonly SiteOptions _site;
    private readonly PublishingOptions _publishing;
    private readonly TimeProvider _clock;

    public ArticleService(
        BlogDbContext db,
        IArticleValidationService validation,
        IDuplicateDetectionService duplicates,
        IRecommendationService recommendations,
        IMarkdownService markdown,
        IAuditService audit,
        IOptions<SiteOptions> site,
        IOptions<PublishingOptions> publishing,
        TimeProvider clock)
    {
        _db = db;
        _validation = validation;
        _duplicates = duplicates;
        _recommendations = recommendations;
        _markdown = markdown;
        _audit = audit;
        _site = site.Value;
        _publishing = publishing.Value;
        _clock = clock;
    }

    public async Task<PagedResult<ArticleListItemDto>> ListAsync(ArticleQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var articles = IncludeList(_db.Articles.AsNoTracking());

        if (query.PublicOnly)
        {
            articles = articles.Where(a => a.Status == ArticleStatus.Published);
        }
        else if (!string.IsNullOrWhiteSpace(query.Status)
                 && Enum.TryParse<ArticleStatus>(query.Status, true, out var status))
        {
            articles = articles.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            articles = articles.Where(a => a.Category.Slug == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            articles = articles.Where(a => a.ArticleTags.Any(t => t.Tag.Slug == query.Tag));
        }

        if (!string.IsNullOrWhiteSpace(query.Technology))
        {
            articles = articles.Where(a => a.ArticleTechnologies.Any(t => t.Technology.Slug == query.Technology));
        }

        if (!string.IsNullOrWhiteSpace(query.Difficulty)
            && Enum.TryParse<Difficulty>(query.Difficulty, true, out var difficulty))
        {
            articles = articles.Where(a => a.Difficulty == difficulty);
        }

        if (!string.IsNullOrWhiteSpace(query.ContentType)
            && Enum.TryParse<ArticleContentType>(query.ContentType, true, out var contentType))
        {
            articles = articles.Where(a => a.ContentType == contentType);
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            articles = articles.Where(a => a.Author.Slug == query.Author);
        }

        if (query.PublishedFrom is not null)
        {
            articles = articles.Where(a => a.PublishedAt >= query.PublishedFrom);
        }

        if (query.PublishedTo is not null)
        {
            articles = articles.Where(a => a.PublishedAt <= query.PublishedTo);
        }

        if (query.FeaturedOnly)
        {
            articles = articles.Where(a => a.IsFeatured);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = $"%{query.Search.Trim()}%";
            articles = articles.Where(a => EF.Functions.ILike(a.Title, like) || EF.Functions.ILike(a.Excerpt, like));
        }

        articles = ApplySort(articles, query.Sort, query.Direction);

        var total = await articles.CountAsync(cancellationToken);
        var items = await articles.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<ArticleListItemDto>(items.Select(ArticleMapper.ToListItem).ToList(), page, pageSize, total);
    }

    public async Task<ArticleDetailDto?> GetByIdAsync(Guid id, bool includeUnpublished, CancellationToken cancellationToken)
    {
        var article = await IncludeDetail(_db.Articles).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (article is null)
        {
            return null;
        }

        if (!includeUnpublished && !article.IsPubliclyVisible)
        {
            return null;
        }

        return await MapDetailAsync(article, cancellationToken);
    }

    public async Task<ArticleDetailDto?> GetBySlugAsync(string slug, bool publicOnly, CancellationToken cancellationToken)
    {
        var article = await IncludeDetail(_db.Articles)
            .FirstOrDefaultAsync(a => a.Slug == slug.ToLowerInvariant(), cancellationToken);
        if (article is null)
        {
            return null;
        }

        if (publicOnly && !article.IsPubliclyVisible)
        {
            return null;
        }

        return await MapDetailAsync(article, cancellationToken);
    }

    public async Task<ArticleDetailDto> CreateAsync(UpsertArticleRequest request, ActorContext actor, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var author = await ResolveAuthorAsync(actor, cancellationToken);
        var category = await ResolveCategoryAsync(request, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(Slugifier.From(request.Slug ?? request.Title), null, cancellationToken);

        var article = Article.Create(
            request.Title,
            slug,
            request.Excerpt,
            request.Content,
            author.Id,
            category.Id,
            now,
            actor.AgentId,
            request.IsAiGenerated || actor.ActorType == ActorType.Agent);

        ApplyEditorial(article, request with { Slug = slug }, category.Id, now);
        await ApplyTaxonomyAsync(article, request, cancellationToken);
        _db.Articles.Add(article);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(actor, AuditActions.ArticleCreated, "article", article.Id.ToString(), new { article.Title, article.Slug }, cancellationToken);
        return (await GetByIdAsync(article.Id, true, cancellationToken))!;
    }

    public async Task<ArticleDetailDto> UpdateAsync(Guid id, UpsertArticleRequest request, ActorContext actor, CancellationToken cancellationToken)
    {
        var article = await IncludeDetail(_db.Articles).FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                      ?? throw AppException.NotFound("Article");

        if (request.RowVersion is not null && request.RowVersion != article.RowVersion)
        {
            throw AppException.Conflict("CONCURRENCY_CONFLICT", "The article was modified by another editor. Reload and try again.");
        }

        var now = _clock.GetUtcNow();
        var category = await ResolveCategoryAsync(request, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(Slugifier.From(request.Slug ?? request.Title), article.Id, cancellationToken);
        var updated = request with { Slug = slug };
        ApplyEditorial(article, updated, category.Id, now);
        await ApplyTaxonomyAsync(article, request, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(actor, AuditActions.ArticleUpdated, "article", article.Id.ToString(), new { article.Title }, cancellationToken);
        return (await GetByIdAsync(article.Id, true, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, ActorContext actor, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                      ?? throw AppException.NotFound("Article");
        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(actor, AuditActions.ArticleDeleted, "article", id.ToString(), new { article.Slug }, cancellationToken);
    }

    public Task<ArticleDetailDto> PublishAsync(Guid id, ActorContext actor, CancellationToken cancellationToken)
        => TransitionAsync(id, actor, AuditActions.ArticlePublished, async article =>
        {
            var validation = await _validation.ValidateAsync(article, cancellationToken);
            if (!validation.Valid)
            {
                throw AppException.Validation("ARTICLE_NOT_READY", "Article is not ready to publish.",
                    validation.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray()));
            }

            var now = _clock.GetUtcNow();
            if (actor.ActorType == ActorType.Agent && IsApprovalRequired())
            {
                article.SubmitForReview(now);
                return;
            }

            article.Publish(now);
        }, cancellationToken);

    public Task<ArticleDetailDto> UnpublishAsync(Guid id, ActorContext actor, CancellationToken cancellationToken)
        => TransitionAsync(id, actor, AuditActions.ArticleUnpublished, article =>
        {
            article.Unpublish(_clock.GetUtcNow());
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<ArticleDetailDto> ScheduleAsync(Guid id, DateTimeOffset scheduledAt, ActorContext actor, CancellationToken cancellationToken)
        => TransitionAsync(id, actor, AuditActions.ArticleScheduled, async article =>
        {
            var validation = await _validation.ValidateAsync(article, cancellationToken);
            if (!validation.Valid)
            {
                throw AppException.Validation("ARTICLE_NOT_READY", "Article is not ready to schedule.",
                    validation.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray()));
            }

            var now = _clock.GetUtcNow();
            if (actor.ActorType == ActorType.Agent && IsApprovalRequired())
            {
                article.SubmitScheduledForReview(scheduledAt, now);
                return;
            }

            article.Schedule(scheduledAt, now);
        }, cancellationToken);

    public Task<ArticleDetailDto> ArchiveAsync(Guid id, ActorContext actor, CancellationToken cancellationToken)
        => TransitionAsync(id, actor, AuditActions.ArticleArchived, article =>
        {
            article.Archive(_clock.GetUtcNow());
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<ArticleDetailDto> SubmitForReviewAsync(Guid id, ActorContext actor, CancellationToken cancellationToken)
        => TransitionAsync(id, actor, AuditActions.ArticleSubmitted, article =>
        {
            article.SubmitForReview(_clock.GetUtcNow());
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<ArticleDetailDto> ApproveAsync(Guid id, ActorContext actor, CancellationToken cancellationToken)
        => TransitionAsync(id, actor, AuditActions.ArticleApproved, async article =>
        {
            var validation = await _validation.ValidateAsync(article, cancellationToken);
            if (!validation.Valid)
            {
                throw AppException.Validation("ARTICLE_NOT_READY", "Article is not ready to approve.",
                    validation.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray()));
            }

            article.Approve(_clock.GetUtcNow());
        }, cancellationToken);

    public Task<ArticleDetailDto> RejectAsync(Guid id, string reason, ActorContext actor, CancellationToken cancellationToken)
        => TransitionAsync(id, actor, AuditActions.ArticleRejected, article =>
        {
            article.Reject(reason, _clock.GetUtcNow());
            return Task.CompletedTask;
        }, cancellationToken);

    public async Task<ArticleValidationResult> ValidateAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await IncludeDetail(_db.Articles).FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                      ?? throw AppException.NotFound("Article");
        return await _validation.ValidateAsync(article, cancellationToken);
    }

    public Task<DuplicateCheckResult> CheckDuplicatesAsync(Guid? articleId, string title, string slug, string content, CancellationToken cancellationToken)
        => _duplicates.CheckAsync(articleId, title, slug, content, cancellationToken);

    public async Task<PreviewResult> CreatePreviewAsync(Guid id, ActorContext actor, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                      ?? throw AppException.NotFound("Article");
        var token = SecretHasher.GenerateToken();
        var now = _clock.GetUtcNow();
        article.SetPreviewToken(SecretHasher.Hash(token), now.AddDays(7), now);
        await _db.SaveChangesAsync(cancellationToken);
        var url = $"{_site.Url.TrimEnd('/')}/preview/{article.Id}?token={token}";
        return new PreviewResult(url, now.AddDays(7));
    }

    public async Task<ArticleDetailDto?> GetByPreviewTokenAsync(string token, CancellationToken cancellationToken)
    {
        var hash = SecretHasher.Hash(token);
        var now = _clock.GetUtcNow();
        var article = await IncludeDetail(_db.Articles)
            .FirstOrDefaultAsync(a => a.PreviewTokenHash == hash && a.PreviewTokenExpiresAt > now, cancellationToken);
        return article is null ? null : await MapDetailAsync(article, cancellationToken);
    }

    public async Task<IReadOnlyList<ArticleListItemDto>> GetRelatedAsync(Guid articleId, int take, CancellationToken cancellationToken)
    {
        var article = await IncludeDetail(_db.Articles).FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken)
                      ?? throw AppException.NotFound("Article");
        return await _recommendations.GetRelatedAsync(article, take, cancellationToken);
    }

    public async Task IncrementViewAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);
        article?.IncrementViews();
        if (article is not null)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> PublishDueScheduledAsync(CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var due = await _db.Articles
            .Where(a => a.Status == ArticleStatus.Scheduled && a.ScheduledAt != null && a.ScheduledAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var article in due)
        {
            if (article.Status != ArticleStatus.Scheduled)
            {
                continue;
            }

            article.Publish(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return due.Count;
    }

    public async Task<string> ExportMarkdownAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await IncludeDetail(_db.Articles).FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                      ?? throw AppException.NotFound("Article");
        var tags = string.Join(", ", article.ArticleTags.Select(t => t.Tag.Name));
        return $"""
                ---
                title: {article.Title}
                slug: {article.Slug}
                category: {article.Category.Slug}
                tags: [{tags}]
                status: {article.Status}
                ---

                {article.Content}
                """;
    }

    public async Task<string> ExportJsonAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await GetByIdAsync(id, true, cancellationToken) ?? throw AppException.NotFound("Article");
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<ArticleDetailDto> ImportMarkdownAsync(string fileName, string markdown, ActorContext actor, CancellationToken cancellationToken)
    {
        var title = Path.GetFileNameWithoutExtension(fileName).Replace('-', ' ');
        var body = markdown;
        if (markdown.StartsWith("---", StringComparison.Ordinal))
        {
            var end = markdown.IndexOf("---", 3, StringComparison.Ordinal);
            if (end > 0)
            {
                var frontMatter = markdown[3..end];
                body = markdown[(end + 3)..].Trim();
                var titleLine = frontMatter.Split('\n').FirstOrDefault(l => l.StartsWith("title:", StringComparison.OrdinalIgnoreCase));
                if (titleLine is not null)
                {
                    title = titleLine.Split(':', 2)[1].Trim().Trim('"');
                }
            }
        }

        var excerpt = body.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#')) ?? title;
        return await CreateAsync(new UpsertArticleRequest(
            title, null, null, excerpt.Trim(), body, null, "software-engineering",
            ["imported"], null, null, null, null, null, "Intermediate", "Guide", false, false,
            null, title, excerpt.Trim()[..Math.Min(excerpt.Trim().Length, 150)], null, null, null, null), actor, cancellationToken);
    }

    private async Task<ArticleDetailDto> TransitionAsync(
        Guid id,
        ActorContext actor,
        string action,
        Func<Article, Task> mutate,
        CancellationToken cancellationToken)
    {
        var article = await IncludeDetail(_db.Articles).FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                      ?? throw AppException.NotFound("Article");
        await mutate(article);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(actor, action, "article", article.Id.ToString(), new { article.Status, article.Slug }, cancellationToken);
        return (await GetByIdAsync(article.Id, true, cancellationToken))!;
    }

    private void ApplyEditorial(Article article, UpsertArticleRequest request, Guid categoryId, DateTimeOffset now)
    {
        var difficulty = Enum.TryParse<Difficulty>(request.Difficulty, true, out var d) ? d : Difficulty.Intermediate;
        var contentType = Enum.TryParse<ArticleContentType>(request.ContentType, true, out var c) ? c : ArticleContentType.Guide;
        article.UpdateEditorial(
            request.Title,
            request.Slug ?? article.Slug,
            request.Subtitle,
            request.Excerpt,
            request.Content,
            categoryId,
            difficulty,
            contentType,
            request.SeriesId,
            request.SeriesOrder,
            request.IsFeatured,
            now);
        article.UpdateSeo(request.MetaTitle, request.MetaDescription, request.CanonicalUrl, request.OgTitle, request.OgDescription, request.OgImage, now);
        article.SetImages(request.FeaturedImageId, request.ThumbnailImageId, now);
    }

    private async Task ApplyTaxonomyAsync(Article article, UpsertArticleRequest request, CancellationToken cancellationToken)
    {
        article.ArticleTags.Clear();
        foreach (var tagName in (request.Tags ?? []).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slug = Slugifier.From(tagName);
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken)
                      ?? _db.Tags.Add(Tag.Create(tagName, slug, null, _clock.GetUtcNow())).Entity;
            article.ArticleTags.Add(new ArticleTag(article.Id, tag.Id));
        }

        article.ArticleTechnologies.Clear();
        foreach (var name in (request.Technologies ?? []).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slug = Slugifier.From(name);
            var tech = await _db.Technologies.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken)
                       ?? _db.Technologies.Add(Technology.Create(name, slug)).Entity;
            article.ArticleTechnologies.Add(new ArticleTechnology(article.Id, tech.Id));
        }
    }

    private async Task<Author> ResolveAuthorAsync(ActorContext actor, CancellationToken cancellationToken)
    {
        if (actor.AuthorId is not null)
        {
            return await _db.Authors.FirstAsync(a => a.Id == actor.AuthorId, cancellationToken);
        }

        return await _db.Authors.FirstOrDefaultAsync(a => a.Slug == _site.DefaultAuthorSlug, cancellationToken)
               ?? await _db.Authors.OrderBy(a => a.CreatedAt).FirstAsync(cancellationToken);
    }

    private async Task<Category> ResolveCategoryAsync(UpsertArticleRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId is not null)
        {
            return await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
                   ?? throw AppException.Validation("CATEGORY_NOT_FOUND", "Category was not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            return await _db.Categories.FirstOrDefaultAsync(c => c.Slug == request.CategorySlug, cancellationToken)
                   ?? throw AppException.Validation("CATEGORY_NOT_FOUND", "Category was not found.");
        }

        throw AppException.Validation("CATEGORY_REQUIRED", "Category is required.");
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "article";
        }

        var candidate = slug;
        var i = 2;
        while (await _db.Articles.AnyAsync(a => a.Slug == candidate && (excludeId == null || a.Id != excludeId), cancellationToken))
        {
            candidate = $"{slug}-{i++}";
        }

        return candidate;
    }

    private async Task<ArticleDetailDto> MapDetailAsync(Article article, CancellationToken cancellationToken)
    {
        var related = await _recommendations.GetRelatedAsync(article, 3, cancellationToken);
        var toc = _markdown.ExtractTableOfContents(article.Content);

        ArticleNavDto? previous = null;
        ArticleNavDto? next = null;
        if (article.PublishedAt is not null)
        {
            var prev = await _db.Articles.AsNoTracking()
                .Where(a => a.Status == ArticleStatus.Published && a.PublishedAt < article.PublishedAt)
                .OrderByDescending(a => a.PublishedAt)
                .Select(a => new ArticleNavDto(a.Id, a.Title, a.Slug))
                .FirstOrDefaultAsync(cancellationToken);
            var nxt = await _db.Articles.AsNoTracking()
                .Where(a => a.Status == ArticleStatus.Published && a.PublishedAt > article.PublishedAt)
                .OrderBy(a => a.PublishedAt)
                .Select(a => new ArticleNavDto(a.Id, a.Title, a.Slug))
                .FirstOrDefaultAsync(cancellationToken);
            previous = prev;
            next = nxt;
        }

        var seriesParts = new List<SeriesPartDto>();
        if (article.SeriesId is not null)
        {
            seriesParts = await _db.Articles.AsNoTracking()
                .Where(a => a.SeriesId == article.SeriesId)
                .OrderBy(a => a.SeriesOrder)
                .Select(a => new SeriesPartDto(a.Id, a.Title, a.Slug, a.SeriesOrder ?? 0, a.Id == article.Id, a.Status == ArticleStatus.Published))
                .ToListAsync(cancellationToken);
        }

        return ArticleMapper.ToDetail(article, toc, related, previous, next, seriesParts);
    }

    private bool IsApprovalRequired()
        => string.Equals(_publishing.Mode, nameof(PublishingMode.RequireApproval), StringComparison.OrdinalIgnoreCase);

    private static IQueryable<Article> IncludeList(IQueryable<Article> query)
        => query.Include(a => a.Author)
            .Include(a => a.Category)
            .Include(a => a.FeaturedImage)
            .Include(a => a.ArticleTags).ThenInclude(t => t.Tag)
            .Include(a => a.ArticleTechnologies).ThenInclude(t => t.Technology);

    private static IQueryable<Article> IncludeDetail(IQueryable<Article> query)
        => IncludeList(query)
            .Include(a => a.Series)
            .Include(a => a.Author).ThenInclude(a => a.Avatar)
            .Include(a => a.ThumbnailImage);

    private static IQueryable<Article> ApplySort(IQueryable<Article> query, string? sort, string? direction)
    {
        var desc = !string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLowerInvariant()) switch
        {
            "title" => desc ? query.OrderByDescending(a => a.Title) : query.OrderBy(a => a.Title),
            "createdat" => desc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
            "updatedat" => desc ? query.OrderByDescending(a => a.UpdatedAt) : query.OrderBy(a => a.UpdatedAt),
            "views" => desc ? query.OrderByDescending(a => a.ViewCount) : query.OrderBy(a => a.ViewCount),
            _ => desc ? query.OrderByDescending(a => a.PublishedAt ?? a.CreatedAt) : query.OrderBy(a => a.PublishedAt ?? a.CreatedAt)
        };
    }
}
