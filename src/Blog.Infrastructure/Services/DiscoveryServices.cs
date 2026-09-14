using Blog.Application.Articles;
using Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Services;

public sealed class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly BlogDbContext _db;

    public DuplicateDetectionService(BlogDbContext db) => _db = db;

    public async Task<DuplicateCheckResult> CheckAsync(
        Guid? articleId,
        string title,
        string slug,
        string content,
        CancellationToken cancellationToken)
    {
        var matches = new List<DuplicateMatchDto>();
        var titleNorm = title.Trim();
        var slugNorm = slug.Trim().ToLowerInvariant();
        var hash = Domain.Entities.Article.ComputeContentHash(content);

        var titleHits = await _db.Articles
            .Where(a => a.Title.ToLower() == titleNorm.ToLower() && (articleId == null || a.Id != articleId))
            .Select(a => new DuplicateMatchDto(a.Id, a.Title, a.Slug, "title", 1.0))
            .Take(5)
            .ToListAsync(cancellationToken);
        matches.AddRange(titleHits);

        var slugHits = await _db.Articles
            .Where(a => (articleId == null || a.Id != articleId) && (a.Slug == slugNorm || a.Slug.StartsWith(slugNorm) || slugNorm.StartsWith(a.Slug)))
            .Select(a => new DuplicateMatchDto(a.Id, a.Title, a.Slug, "slug", a.Slug == slugNorm ? 1.0 : 0.7))
            .Take(5)
            .ToListAsync(cancellationToken);
        matches.AddRange(slugHits.Where(h => matches.All(m => m.ArticleId != h.ArticleId)));

        var contentHits = await _db.Articles
            .Where(a => a.ContentHash == hash && (articleId == null || a.Id != articleId))
            .Select(a => new DuplicateMatchDto(a.Id, a.Title, a.Slug, "content", 1.0))
            .Take(5)
            .ToListAsync(cancellationToken);
        matches.AddRange(contentHits.Where(h => matches.All(m => m.ArticleId != h.ArticleId)));

        return new DuplicateCheckResult(matches.Count > 0, matches);
    }
}

public sealed class RecommendationService : IRecommendationService
{
    private readonly BlogDbContext _db;

    public RecommendationService(BlogDbContext db) => _db = db;

    public async Task<IReadOnlyList<ArticleListItemDto>> GetRelatedAsync(Domain.Entities.Article article, int take, CancellationToken cancellationToken)
    {
        var tagIds = article.ArticleTags.Select(t => t.TagId).ToList();
        var techIds = article.ArticleTechnologies.Select(t => t.TechnologyId).ToList();

        var candidates = await _db.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .Include(a => a.Category)
            .Include(a => a.FeaturedImage)
            .Include(a => a.ArticleTags).ThenInclude(t => t.Tag)
            .Include(a => a.ArticleTechnologies).ThenInclude(t => t.Technology)
            .Where(a => a.Id != article.Id && a.Status == Domain.Enums.ArticleStatus.Published)
            .Take(80)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(c => new
            {
                Article = c,
                Score =
                    (c.CategoryId == article.CategoryId ? 3 : 0)
                    + (c.SeriesId is not null && c.SeriesId == article.SeriesId ? 4 : 0)
                    + c.ArticleTags.Count(t => tagIds.Contains(t.TagId))
                    + c.ArticleTechnologies.Count(t => techIds.Contains(t.TechnologyId))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Article.PublishedAt)
            .Take(take)
            .Select(x => Mapping.ArticleMapper.ToListItem(x.Article))
            .ToList();
    }
}
