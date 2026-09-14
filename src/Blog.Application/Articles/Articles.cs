using Blog.Application.Common;
using Blog.Domain.Enums;

namespace Blog.Application.Articles;

public sealed record ArticleListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Excerpt,
    string Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int ReadingTimeMinutes,
    long ViewCount,
    bool IsFeatured,
    bool IsAiGenerated,
    string CategoryName,
    string CategorySlug,
    string AuthorName,
    string AuthorSlug,
    string? FeaturedImageUrl,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Technologies,
    string Difficulty,
    string ContentType);

public sealed record ArticleDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Subtitle,
    string Excerpt,
    string Content,
    string ContentFormat,
    string Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? FeaturedImageId,
    string? FeaturedImageUrl,
    Guid? ThumbnailImageId,
    string? CanonicalUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? OgTitle,
    string? OgDescription,
    string? OgImage,
    int ReadingTimeMinutes,
    long ViewCount,
    bool IsFeatured,
    bool IsAiGenerated,
    Guid AuthorId,
    string AuthorName,
    string AuthorSlug,
    string? AuthorBio,
    string? AuthorAvatarUrl,
    string? AuthorGitHub,
    string? AuthorLinkedIn,
    string? AuthorX,
    string? AuthorWebsite,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    Guid? SeriesId,
    string? SeriesName,
    string? SeriesSlug,
    int? SeriesOrder,
    string Difficulty,
    string ContentType,
    string? RejectionReason,
    uint RowVersion,
    IReadOnlyList<TagRefDto> Tags,
    IReadOnlyList<TagRefDto> Technologies,
    IReadOnlyList<TocItemDto> TableOfContents,
    IReadOnlyList<ArticleListItemDto> Related,
    ArticleNavDto? Previous,
    ArticleNavDto? Next,
    IReadOnlyList<SeriesPartDto> SeriesParts);

public sealed record ArticleNavDto(Guid Id, string Title, string Slug);
public sealed record SeriesPartDto(Guid Id, string Title, string Slug, int Order, bool IsCurrent, bool IsPublished);
public sealed record TagRefDto(Guid Id, string Name, string Slug);
public sealed record TocItemDto(int Level, string Text, string Id);

public sealed record UpsertArticleRequest(
    string Title,
    string? Slug,
    string? Subtitle,
    string Excerpt,
    string Content,
    Guid? CategoryId,
    string? CategorySlug,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? Technologies,
    Guid? FeaturedImageId,
    Guid? ThumbnailImageId,
    Guid? SeriesId,
    int? SeriesOrder,
    string? Difficulty,
    string? ContentType,
    bool IsFeatured,
    bool IsAiGenerated,
    string? CanonicalUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? OgTitle,
    string? OgDescription,
    string? OgImage,
    uint? RowVersion);

public sealed record ScheduleArticleRequest(DateTimeOffset ScheduledAt);
public sealed record RejectArticleRequest(string Reason);

public sealed record ArticleQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? Category = null,
    string? Tag = null,
    string? Technology = null,
    string? Difficulty = null,
    string? ContentType = null,
    string? Author = null,
    DateTimeOffset? PublishedFrom = null,
    DateTimeOffset? PublishedTo = null,
    string? Sort = "publishedAt",
    string? Direction = "desc",
    string? Search = null,
    bool FeaturedOnly = false,
    bool PublicOnly = false);

public sealed record ValidationIssue(string Code, string Message, string Severity);

public sealed record ArticleValidationResult(
    bool Valid,
    bool ReadyToPublish,
    int Score,
    int SeoScore,
    int ContentScore,
    IReadOnlyList<ValidationIssue> Errors,
    IReadOnlyList<ValidationIssue> Warnings,
    IReadOnlyList<string> MissingMetadata);

public sealed record DuplicateCheckResult(
    bool HasDuplicates,
    IReadOnlyList<DuplicateMatchDto> Matches);

public sealed record DuplicateMatchDto(
    Guid ArticleId,
    string Title,
    string Slug,
    string MatchType,
    double Score);

public sealed record PreviewResult(string PreviewUrl, DateTimeOffset ExpiresAt);

public sealed record PublishingRulesDto(
    string PublishingMode,
    bool ApprovalRequired,
    int MinimumContentLength,
    bool RequireSeoTitle,
    bool RequireMetaDescription,
    bool RequireFeaturedImage,
    bool RequireCategory,
    int MinimumTagCount,
    IReadOnlyList<string> AllowedContentFormats,
    IReadOnlyList<string> AllowedCategories,
    IReadOnlyList<string> AllowedDifficulties,
    IReadOnlyList<string> AllowedContentTypes,
    string AuthorIdentity,
    SeoRuleDto Seo,
    ScheduleRuleDto Schedule);

public sealed record SeoRuleDto(
    int TitleMaxLength,
    int MetaDescriptionMinLength,
    int MetaDescriptionMaxLength);

public sealed record ScheduleRuleDto(string TimeZone, string PreferredHourUtc);

public interface IArticleService
{
    Task<PagedResult<ArticleListItemDto>> ListAsync(ArticleQuery query, CancellationToken cancellationToken);
    Task<ArticleDetailDto?> GetByIdAsync(Guid id, bool includeUnpublished, CancellationToken cancellationToken);
    Task<ArticleDetailDto?> GetBySlugAsync(string slug, bool publicOnly, CancellationToken cancellationToken);
    Task<ArticleDetailDto> CreateAsync(UpsertArticleRequest request, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto> UpdateAsync(Guid id, UpsertArticleRequest request, ActorContext actor, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto> PublishAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto> UnpublishAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto> ScheduleAsync(Guid id, DateTimeOffset scheduledAt, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto> ArchiveAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto> SubmitForReviewAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto> ApproveAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto> RejectAsync(Guid id, string reason, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleValidationResult> ValidateAsync(Guid id, CancellationToken cancellationToken);
    Task<DuplicateCheckResult> CheckDuplicatesAsync(Guid? articleId, string title, string slug, string content, CancellationToken cancellationToken);
    Task<PreviewResult> CreatePreviewAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);
    Task<ArticleDetailDto?> GetByPreviewTokenAsync(string token, CancellationToken cancellationToken);
    Task<IReadOnlyList<ArticleListItemDto>> GetRelatedAsync(Guid articleId, int take, CancellationToken cancellationToken);
    Task IncrementViewAsync(Guid articleId, CancellationToken cancellationToken);
    Task<int> PublishDueScheduledAsync(CancellationToken cancellationToken);
    Task<string> ExportMarkdownAsync(Guid id, CancellationToken cancellationToken);
    Task<string> ExportJsonAsync(Guid id, CancellationToken cancellationToken);
    Task<ArticleDetailDto> ImportMarkdownAsync(string fileName, string markdown, ActorContext actor, CancellationToken cancellationToken);
}

public interface IArticleValidationService
{
    Task<ArticleValidationResult> ValidateAsync(Domain.Entities.Article article, CancellationToken cancellationToken);
}

public interface IRecommendationService
{
    Task<IReadOnlyList<ArticleListItemDto>> GetRelatedAsync(Domain.Entities.Article article, int take, CancellationToken cancellationToken);
}

public interface IDuplicateDetectionService
{
    Task<DuplicateCheckResult> CheckAsync(Guid? articleId, string title, string slug, string content, CancellationToken cancellationToken);
}
