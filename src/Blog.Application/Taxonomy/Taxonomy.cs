using Blog.Application.Common;

namespace Blog.Application.Taxonomy;

public sealed record CategoryDto(Guid Id, string Name, string Slug, string? Description, int SortOrder, bool IsActive, int ArticleCount);
public sealed record TagDto(Guid Id, string Name, string Slug, string? Description, int ArticleCount);
public sealed record TechnologyDto(Guid Id, string Name, string Slug, int ArticleCount);
public sealed record SeriesDto(Guid Id, string Name, string Slug, string? Description, Guid? CoverImageId, int ArticleCount);
public sealed record SeriesDetailDto(Guid Id, string Name, string Slug, string? Description, IReadOnlyList<SeriesArticleDto> Articles);
public sealed record SeriesArticleDto(Guid Id, string Title, string Slug, int? Order, string Status, DateTimeOffset? PublishedAt);
public sealed record AuthorDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string Bio,
    string? AvatarUrl,
    string? GitHubUrl,
    string? LinkedInUrl,
    string? XUrl,
    string? WebsiteUrl,
    int ArticleCount);
public sealed record UpsertCategoryRequest(string Name, string? Slug, string? Description, int SortOrder, bool IsActive = true);
public sealed record UpsertTagRequest(string Name, string? Slug, string? Description);
public sealed record UpsertSeriesRequest(string Name, string? Slug, string? Description, Guid? CoverImageId);
public sealed record UpsertAuthorRequest(
    string DisplayName,
    string? Slug,
    string Bio,
    Guid? AvatarMediaId,
    string? GitHubUrl,
    string? LinkedInUrl,
    string? XUrl,
    string? WebsiteUrl);

public interface ITaxonomyService
{
    Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<CategoryDto> CreateCategoryAsync(UpsertCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpsertCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<TagDto>> ListTagsAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);
    Task<TagDto?> GetTagBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<TagDto> CreateTagAsync(UpsertTagRequest request, CancellationToken cancellationToken);
    Task<TagDto> UpdateTagAsync(Guid id, UpsertTagRequest request, CancellationToken cancellationToken);
    Task DeleteTagAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TechnologyDto>> ListTechnologiesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SeriesDto>> ListSeriesAsync(CancellationToken cancellationToken);
    Task<SeriesDetailDto?> GetSeriesBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<SeriesDto> CreateSeriesAsync(UpsertSeriesRequest request, CancellationToken cancellationToken);
    Task<SeriesDto> UpdateSeriesAsync(Guid id, UpsertSeriesRequest request, CancellationToken cancellationToken);
    Task DeleteSeriesAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuthorDto>> ListAuthorsAsync(CancellationToken cancellationToken);
    Task<AuthorDto?> GetAuthorBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<AuthorDto> CreateAuthorAsync(UpsertAuthorRequest request, CancellationToken cancellationToken);
    Task<AuthorDto> UpdateAuthorAsync(Guid id, UpsertAuthorRequest request, CancellationToken cancellationToken);
}
