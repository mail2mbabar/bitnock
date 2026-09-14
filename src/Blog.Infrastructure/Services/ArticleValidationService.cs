using Blog.Application.Articles;
using Blog.Application.Auth;
using Blog.Application.Options;
using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Blog.Infrastructure.Services;

public sealed class ArticleValidationService : IArticleValidationService
{
    private readonly PublishingOptions _options;
    private readonly IMarkdownService _markdown;

    public ArticleValidationService(IOptions<PublishingOptions> options, IMarkdownService markdown)
    {
        _options = options.Value;
        _markdown = markdown;
    }

    public Task<ArticleValidationResult> ValidateAsync(Article article, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationIssue>();
        var warnings = new List<ValidationIssue>();
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(article.Title))
        {
            errors.Add(new("TITLE_REQUIRED", "Title is required.", "error"));
            missing.Add("title");
        }

        if (string.IsNullOrWhiteSpace(article.Slug))
        {
            errors.Add(new("SLUG_REQUIRED", "Slug is required.", "error"));
            missing.Add("slug");
        }

        if (string.IsNullOrWhiteSpace(article.Content))
        {
            errors.Add(new("CONTENT_REQUIRED", "Content is required.", "error"));
            missing.Add("content");
        }
        else if (article.Content.Trim().Length < _options.MinimumContentLength)
        {
            errors.Add(new("CONTENT_TOO_SHORT", $"Content must be at least {_options.MinimumContentLength} characters.", "error"));
        }

        if (article.CategoryId == Guid.Empty)
        {
            errors.Add(new("CATEGORY_REQUIRED", "Category is required.", "error"));
            missing.Add("category");
        }

        if (article.AuthorId == Guid.Empty)
        {
            errors.Add(new("AUTHOR_REQUIRED", "Author is required.", "error"));
            missing.Add("author");
        }

        if (_options.RequireSeoTitle && string.IsNullOrWhiteSpace(article.MetaTitle))
        {
            errors.Add(new("SEO_TITLE_MISSING", "SEO title is required.", "error"));
            missing.Add("metaTitle");
        }

        if (_options.RequireMetaDescription && string.IsNullOrWhiteSpace(article.MetaDescription))
        {
            errors.Add(new("SEO_DESCRIPTION_MISSING", "SEO description is required.", "error"));
            missing.Add("metaDescription");
        }
        else if (!string.IsNullOrWhiteSpace(article.MetaDescription))
        {
            if (article.MetaDescription.Length < _options.MetaDescriptionMinLength)
            {
                warnings.Add(new("SEO_DESCRIPTION_SHORT", "Meta description is shorter than recommended.", "warning"));
            }

            if (article.MetaDescription.Length > _options.MetaDescriptionMaxLength)
            {
                errors.Add(new("SEO_DESCRIPTION_TOO_LONG", "Meta description exceeds the maximum length.", "error"));
            }
        }

        if (_options.RequireFeaturedImage && article.FeaturedImageId is null)
        {
            errors.Add(new("FEATURED_IMAGE_REQUIRED", "A featured image is required before publishing.", "error"));
            missing.Add("featuredImage");
        }

        if (article.ArticleTags.Count < _options.MinimumTagCount)
        {
            errors.Add(new("TAGS_REQUIRED", $"At least {_options.MinimumTagCount} tag(s) are required.", "error"));
            missing.Add("tags");
        }

        if (!_options.AllowHtmlContent && article.ContentFormat != ContentFormat.Markdown)
        {
            errors.Add(new("FORMAT_NOT_ALLOWED", "Only Markdown content is allowed.", "error"));
        }

        if (article.Content.Contains("<script", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new("PROHIBITED_HTML", "Script tags are not allowed in article content.", "error"));
        }

        var links = _markdown.ExtractLinks(article.Content);
        var malformed = links.Where(l => !Uri.TryCreate(l, UriKind.RelativeOrAbsolute, out _)).ToList();
        if (malformed.Count > 0)
        {
            warnings.Add(new("INVALID_LINKS", $"{malformed.Count} link(s) may be malformed.", "warning"));
        }

        if (!article.Content.Contains("](/articles/", StringComparison.OrdinalIgnoreCase)
            && !article.Content.Contains("](https://", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new("INTERNAL_LINKS", "Consider adding at least one internal link.", "warning"));
        }

        var headingCount = article.Content.Split('\n').Count(l => l.StartsWith("## ", StringComparison.Ordinal));
        if (headingCount < 2)
        {
            warnings.Add(new("STRUCTURE", "Consider adding more H2 headings for scanability.", "warning"));
        }

        var codeBlocks = article.Content.Split("```").Length - 1;
        if (article.ContentType is ArticleContentType.Tutorial or ArticleContentType.CodeExample or ArticleContentType.DeepDive
            && codeBlocks < 2)
        {
            warnings.Add(new("CODE_EXAMPLES", "Technical articles usually need more code examples.", "warning"));
        }

        var contentScore = Math.Clamp(
            40
            + Math.Min(30, article.Content.Length / 80)
            + Math.Min(15, headingCount * 5)
            + Math.Min(15, codeBlocks * 5)
            - errors.Count * 10,
            0,
            100);

        var seoScore = 100;
        if (string.IsNullOrWhiteSpace(article.MetaTitle)) seoScore -= 25;
        if (string.IsNullOrWhiteSpace(article.MetaDescription)) seoScore -= 25;
        if (article.FeaturedImageId is null) seoScore -= 15;
        if (string.IsNullOrWhiteSpace(article.CanonicalUrl)) seoScore -= 5;
        if (article.ArticleTags.Count == 0) seoScore -= 10;
        seoScore = Math.Clamp(seoScore - warnings.Count * 2, 0, 100);

        var score = (int)Math.Round((contentScore * 0.6) + (seoScore * 0.4));
        var valid = errors.Count == 0;

        return Task.FromResult(new ArticleValidationResult(
            valid,
            valid,
            score,
            seoScore,
            contentScore,
            errors,
            warnings,
            missing));
    }
}
