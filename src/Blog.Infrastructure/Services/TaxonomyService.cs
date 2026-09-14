using Blog.Application.Common;
using Blog.Application.Taxonomy;
using Blog.Domain.Entities;
using Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Services;

public sealed class TaxonomyService : ITaxonomyService
{
    private readonly BlogDbContext _db;
    private readonly TimeProvider _clock;

    public TaxonomyService(BlogDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.SortOrder, c.IsActive, c.Articles.Count(a => a.Status == Domain.Enums.ArticleStatus.Published)))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken)
        => (await ListCategoriesAsync(false, cancellationToken)).FirstOrDefault(c => c.Slug == slug);

    public async Task<CategoryDto> CreateCategoryAsync(UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var entity = Category.Create(request.Name, Slugifier.From(request.Slug ?? request.Name), request.Description, request.SortOrder, now);
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new CategoryDto(entity.Id, entity.Name, entity.Slug, entity.Description, entity.SortOrder, entity.IsActive, 0);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw AppException.NotFound("Category");
        entity.Update(request.Name, Slugifier.From(request.Slug ?? request.Name), request.Description, request.SortOrder, request.IsActive, _clock.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);
        return new CategoryDto(entity.Id, entity.Name, entity.Slug, entity.Description, entity.SortOrder, entity.IsActive, 0);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var used = await _db.Articles.AnyAsync(a => a.CategoryId == id, cancellationToken);
        if (used)
        {
            throw AppException.Conflict("CATEGORY_IN_USE", "Category is assigned to articles and cannot be deleted.");
        }

        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw AppException.NotFound("Category");
        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<TagDto>> ListTagsAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Tags.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search}%";
            query = query.Where(t => EF.Functions.ILike(t.Name, like) || EF.Functions.ILike(t.Slug, like));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TagDto(t.Id, t.Name, t.Slug, t.Description, t.ArticleTags.Count(at => at.Article.Status == Domain.Enums.ArticleStatus.Published)))
            .ToListAsync(cancellationToken);
        return new PagedResult<TagDto>(items, page, pageSize, total);
    }

    public async Task<TagDto?> GetTagBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await _db.Tags.AsNoTracking()
            .Where(t => t.Slug == slug)
            .Select(t => new TagDto(t.Id, t.Name, t.Slug, t.Description, t.ArticleTags.Count(at => at.Article.Status == Domain.Enums.ArticleStatus.Published)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TagDto> CreateTagAsync(UpsertTagRequest request, CancellationToken cancellationToken)
    {
        var entity = Tag.Create(request.Name, Slugifier.From(request.Slug ?? request.Name), request.Description, _clock.GetUtcNow());
        _db.Tags.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new TagDto(entity.Id, entity.Name, entity.Slug, entity.Description, 0);
    }

    public async Task<TagDto> UpdateTagAsync(Guid id, UpsertTagRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken) ?? throw AppException.NotFound("Tag");
        entity.Update(request.Name, Slugifier.From(request.Slug ?? request.Name), request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return new TagDto(entity.Id, entity.Name, entity.Slug, entity.Description, 0);
    }

    public async Task DeleteTagAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken) ?? throw AppException.NotFound("Tag");
        _db.Tags.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TechnologyDto>> ListTechnologiesAsync(CancellationToken cancellationToken)
        => await _db.Technologies.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TechnologyDto(t.Id, t.Name, t.Slug, t.ArticleTechnologies.Count))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SeriesDto>> ListSeriesAsync(CancellationToken cancellationToken)
        => await _db.Series.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SeriesDto(s.Id, s.Name, s.Slug, s.Description, s.CoverImageId, s.Articles.Count))
            .ToListAsync(cancellationToken);

    public async Task<SeriesDetailDto?> GetSeriesBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var series = await _db.Series.AsNoTracking()
            .Include(s => s.Articles)
            .FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);
        if (series is null)
        {
            return null;
        }

        var articles = series.Articles
            .OrderBy(a => a.SeriesOrder)
            .Select(a => new SeriesArticleDto(a.Id, a.Title, a.Slug, a.SeriesOrder, a.Status.ToString(), a.PublishedAt))
            .ToList();
        return new SeriesDetailDto(series.Id, series.Name, series.Slug, series.Description, articles);
    }

    public async Task<SeriesDto> CreateSeriesAsync(UpsertSeriesRequest request, CancellationToken cancellationToken)
    {
        var entity = Series.Create(request.Name, Slugifier.From(request.Slug ?? request.Name), request.Description, request.CoverImageId, _clock.GetUtcNow());
        _db.Series.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new SeriesDto(entity.Id, entity.Name, entity.Slug, entity.Description, entity.CoverImageId, 0);
    }

    public async Task<SeriesDto> UpdateSeriesAsync(Guid id, UpsertSeriesRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Series.FirstOrDefaultAsync(s => s.Id == id, cancellationToken) ?? throw AppException.NotFound("Series");
        entity.Update(request.Name, Slugifier.From(request.Slug ?? request.Name), request.Description, request.CoverImageId, _clock.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);
        return new SeriesDto(entity.Id, entity.Name, entity.Slug, entity.Description, entity.CoverImageId, entity.Articles.Count);
    }

    public async Task DeleteSeriesAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.Series.FirstOrDefaultAsync(s => s.Id == id, cancellationToken) ?? throw AppException.NotFound("Series");
        _db.Series.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthorDto>> ListAuthorsAsync(CancellationToken cancellationToken)
        => await _db.Authors.AsNoTracking()
            .Include(a => a.Avatar)
            .OrderBy(a => a.DisplayName)
            .Select(a => new AuthorDto(a.Id, a.DisplayName, a.Slug, a.Bio, a.Avatar != null ? a.Avatar.Url : null, a.GitHubUrl, a.LinkedInUrl, a.XUrl, a.WebsiteUrl, a.Articles.Count(x => x.Status == Domain.Enums.ArticleStatus.Published)))
            .ToListAsync(cancellationToken);

    public async Task<AuthorDto?> GetAuthorBySlugAsync(string slug, CancellationToken cancellationToken)
        => (await ListAuthorsAsync(cancellationToken)).FirstOrDefault(a => a.Slug == slug);

    public async Task<AuthorDto> CreateAuthorAsync(UpsertAuthorRequest request, CancellationToken cancellationToken)
    {
        var entity = Author.Create(request.DisplayName, Slugifier.From(request.Slug ?? request.DisplayName), request.Bio, _clock.GetUtcNow(), null, request.GitHubUrl, request.LinkedInUrl, request.XUrl, request.WebsiteUrl);
        if (request.AvatarMediaId is not null)
        {
            entity.Update(entity.DisplayName, entity.Slug, entity.Bio, request.AvatarMediaId, entity.GitHubUrl, entity.LinkedInUrl, entity.XUrl, entity.WebsiteUrl, _clock.GetUtcNow());
        }

        _db.Authors.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new AuthorDto(entity.Id, entity.DisplayName, entity.Slug, entity.Bio, null, entity.GitHubUrl, entity.LinkedInUrl, entity.XUrl, entity.WebsiteUrl, 0);
    }

    public async Task<AuthorDto> UpdateAuthorAsync(Guid id, UpsertAuthorRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Authors.FirstOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw AppException.NotFound("Author");
        entity.Update(request.DisplayName, Slugifier.From(request.Slug ?? request.DisplayName), request.Bio, request.AvatarMediaId, request.GitHubUrl, request.LinkedInUrl, request.XUrl, request.WebsiteUrl, _clock.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);
        return new AuthorDto(entity.Id, entity.DisplayName, entity.Slug, entity.Bio, null, entity.GitHubUrl, entity.LinkedInUrl, entity.XUrl, entity.WebsiteUrl, 0);
    }
}
