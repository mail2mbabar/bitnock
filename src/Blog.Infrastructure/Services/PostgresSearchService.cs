using Blog.Application.Articles;
using Blog.Application.Auth;
using Blog.Application.Common;
using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Infrastructure.Mapping;
using Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Services;

public sealed class PostgresSearchService : ISearchService
{
    private readonly BlogDbContext _db;

    public PostgresSearchService(BlogDbContext db) => _db = db;

    public async Task<PagedResult<ArticleListItemDto>> SearchAsync(string query, ArticleQuery filters, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, filters.Page);
        var pageSize = Math.Clamp(filters.PageSize, 1, 50);
        var parsed = SearchLexicon.Parse(query);

        var articles = await _db.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .Include(a => a.Category)
            .Include(a => a.FeaturedImage)
            .Include(a => a.ArticleTags).ThenInclude(t => t.Tag)
            .Include(a => a.ArticleTechnologies).ThenInclude(t => t.Technology)
            .Where(a => a.Status == ArticleStatus.Published)
            .ToListAsync(cancellationToken);

        IEnumerable<Article> filtered = articles;
        if (!string.IsNullOrWhiteSpace(filters.Category))
        {
            filtered = filtered.Where(a => a.Category.Slug == filters.Category);
        }

        if (!string.IsNullOrWhiteSpace(filters.Tag))
        {
            filtered = filtered.Where(a => a.ArticleTags.Any(t => t.Tag.Slug == filters.Tag));
        }

        if (!string.IsNullOrWhiteSpace(filters.Technology))
        {
            filtered = filtered.Where(a => a.ArticleTechnologies.Any(t => t.Technology.Slug == filters.Technology));
        }

        var ranked = filtered
            .Select(article => (article, score: Score(article, parsed)))
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.article.PublishedAt)
            .ToList();

        if (!string.IsNullOrWhiteSpace(parsed.Original) && ranked.TrueForAll(x => x.score <= 0))
        {
            ranked = filtered
                .Select(article => (article, score: SearchLexicon.Similarity(parsed.Original, article.Title) * 40
                    + SearchLexicon.Similarity(parsed.Original, article.Excerpt) * 20
                    + SearchLexicon.Similarity(parsed.Original, article.Category.Name) * 15))
                .OrderByDescending(x => x.score)
                .ToList();
        }

        var top = ranked.Count == 0 ? 0 : ranked[0].score;
        var floor = top <= 0 ? 0 : Math.Max(6, top * 0.16);
        var meaningful = ranked.Where(x => x.score >= floor).ToList();
        if (meaningful.Count == 0)
        {
            meaningful = ranked.Take(Math.Min(8, ranked.Count)).ToList();
        }
        else if (meaningful.Count < 3)
        {
            meaningful = ranked.Take(Math.Min(8, ranked.Count)).ToList();
        }

        var total = meaningful.Count;
        var pageItems = meaningful
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ArticleMapper.ToListItem(x.article))
            .ToList();

        return new PagedResult<ArticleListItemDto>(pageItems, page, pageSize, total);
    }

    private static double Score(Article article, SearchQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Original))
        {
            return article.IsFeatured ? 1 : 0;
        }

        var hayTitle = Hay(article.Title, article.Subtitle, article.Slug.Replace('-', ' '));
        var hayMeta = Hay(article.Category.Name, article.Category.Slug,
            string.Join(' ', article.ArticleTags.Select(t => t.Tag.Name)),
            string.Join(' ', article.ArticleTechnologies.Select(t => t.Technology.Name)));
        var hayExcerpt = Hay(article.Excerpt);
        var hayBody = Hay(article.Content);

        double score = 0;
        var normalizedQuery = query.Normalized;
        if (SearchLexicon.Hits(hayTitle, normalizedQuery))
        {
            score += 140;
        }

        if (SearchLexicon.Hits(hayExcerpt, normalizedQuery))
        {
            score += 36;
        }

        score += SearchLexicon.Similarity(query.Original, article.Title) * 80;
        score += SearchLexicon.Similarity(query.Original, article.Excerpt) * 18;

        var core = new HashSet<string>(query.Core, StringComparer.OrdinalIgnoreCase);
        var related = new HashSet<string>(query.Related, StringComparer.OrdinalIgnoreCase);
        foreach (var term in query.Terms)
        {
            var t = SearchLexicon.Normalize(term);
            if (t.Length < 2)
            {
                continue;
            }

            var weight = core.Contains(term) || core.Contains(t) ? 1 : related.Contains(term) || related.Contains(t) ? 2 : 3;
            var titleHit = SearchLexicon.Hits(hayTitle, t);
            var metaHit = SearchLexicon.Hits(hayMeta, t);
            var excerptHit = SearchLexicon.Hits(hayExcerpt, t);
            var bodyHit = SearchLexicon.Hits(hayBody, t);

            if (titleHit)
            {
                score += weight switch { 1 => t.Length > 8 ? 56 : 40, 2 => 34, _ => 10 };
            }

            if (metaHit)
            {
                score += weight switch { 1 => 32, 2 => 18, _ => 6 };
            }

            if (excerptHit)
            {
                score += weight switch { 1 => 16, 2 => 10, _ => 3 };
            }

            if (bodyHit)
            {
                score += weight switch { 1 => 8, 2 => 4, _ => 1 };
            }
        }

        if (core.Overlaps(["secure", "protect", "login", "log", "auth", "password", "signin", "sign"])
            && (SearchLexicon.Hits(hayTitle, "jwt") || SearchLexicon.Hits(hayTitle, "authentication") || SearchLexicon.Hits(hayTitle, "authorization")))
        {
            score += 140;
        }

        if (core.Overlaps(["slow", "speed", "lag", "performance"])
            && (SearchLexicon.Hits(hayTitle, "performance") || SearchLexicon.Hits(hayTitle, "ef core")))
        {
            score += 50;
        }

        if (article.IsFeatured)
        {
            score += 4;
        }

        return score;
    }

    private static string Hay(params string?[] parts) =>
        SearchLexicon.Normalize(string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p))!));
}
