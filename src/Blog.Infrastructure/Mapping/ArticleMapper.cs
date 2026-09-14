using Blog.Application.Articles;
using Blog.Application.Common;
using Blog.Domain.Entities;

namespace Blog.Infrastructure.Mapping;

internal static class ArticleMapper
{
    public static ArticleListItemDto ToListItem(Article article)
    {
        return new ArticleListItemDto(
            article.Id,
            article.Title,
            article.Slug,
            EditorialText.WithoutAiDashes(article.Excerpt),
            article.Status.ToString(),
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt,
            article.ReadingTimeMinutes,
            article.ViewCount,
            article.IsFeatured,
            article.IsAiGenerated,
            article.Category.Name,
            article.Category.Slug,
            article.Author.DisplayName,
            article.Author.Slug,
            article.FeaturedImage?.Url,
            article.ArticleTags.Select(x => x.Tag.Name).ToList(),
            article.ArticleTechnologies.Select(x => x.Technology.Name).ToList(),
            article.Difficulty.ToString(),
            article.ContentType.ToString());
    }

    public static ArticleDetailDto ToDetail(
        Article article,
        IReadOnlyList<TocItemDto> toc,
        IReadOnlyList<ArticleListItemDto> related,
        ArticleNavDto? previous,
        ArticleNavDto? next,
        IReadOnlyList<SeriesPartDto> seriesParts)
    {
        return new ArticleDetailDto(
            article.Id,
            article.Title,
            article.Slug,
            EditorialText.WithoutAiDashes(article.Subtitle),
            EditorialText.WithoutAiDashes(article.Excerpt),
            EditorialText.WithoutAiDashes(article.Content),
            article.ContentFormat.ToString(),
            article.Status.ToString(),
            article.PublishedAt,
            article.ScheduledAt,
            article.CreatedAt,
            article.UpdatedAt,
            article.FeaturedImageId,
            article.FeaturedImage?.Url,
            article.ThumbnailImageId,
            article.CanonicalUrl,
            article.MetaTitle,
            article.MetaDescription,
            article.OgTitle,
            article.OgDescription,
            article.OgImage,
            article.ReadingTimeMinutes,
            article.ViewCount,
            article.IsFeatured,
            article.IsAiGenerated,
            article.AuthorId,
            article.Author.DisplayName,
            article.Author.Slug,
            article.Author.Bio,
            article.Author.Avatar?.Url,
            article.Author.GitHubUrl,
            article.Author.LinkedInUrl,
            article.Author.XUrl,
            article.Author.WebsiteUrl,
            article.CategoryId,
            article.Category.Name,
            article.Category.Slug,
            article.SeriesId,
            article.Series?.Name,
            article.Series?.Slug,
            article.SeriesOrder,
            article.Difficulty.ToString(),
            article.ContentType.ToString(),
            article.RejectionReason,
            article.RowVersion,
            article.ArticleTags.Select(x => new TagRefDto(x.Tag.Id, x.Tag.Name, x.Tag.Slug)).ToList(),
            article.ArticleTechnologies.Select(x => new TagRefDto(x.Technology.Id, x.Technology.Name, x.Technology.Slug)).ToList(),
            toc,
            related,
            previous,
            next,
            seriesParts);
    }
}
