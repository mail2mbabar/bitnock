using Blog.Domain.Common;
using Blog.Domain.Enums;

namespace Blog.Domain.Entities;

public sealed class Article : Entity
{
    public const int MinContentLength = 400;
    public const int DefaultWordsPerMinute = 220;

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public string Excerpt { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public ContentFormat ContentFormat { get; private set; } = ContentFormat.Markdown;
    public ArticleStatus Status { get; private set; } = ArticleStatus.Draft;
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ScheduledAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? FeaturedImageId { get; private set; }
    public Guid? ThumbnailImageId { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImage { get; private set; }
    public int ReadingTimeMinutes { get; private set; }
    public long ViewCount { get; private set; }
    public bool IsFeatured { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? SeriesId { get; private set; }
    public int? SeriesOrder { get; private set; }
    public Difficulty Difficulty { get; private set; } = Difficulty.Intermediate;
    public ArticleContentType ContentType { get; private set; } = ArticleContentType.Guide;
    public string? RejectionReason { get; private set; }
    public Guid? CreatedByAgentId { get; private set; }
    public bool IsAiGenerated { get; private set; }
    public string? PreviewTokenHash { get; private set; }
    public DateTimeOffset? PreviewTokenExpiresAt { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public uint RowVersion { get; private set; }

    public Author Author { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
    public Series? Series { get; private set; }
    public MediaAsset? FeaturedImage { get; private set; }
    public MediaAsset? ThumbnailImage { get; private set; }
    public ICollection<ArticleTag> ArticleTags { get; private set; } = new List<ArticleTag>();
    public ICollection<ArticleTechnology> ArticleTechnologies { get; private set; } = new List<ArticleTechnology>();
    public ICollection<RelatedArticleLink> RelatedFrom { get; private set; } = new List<RelatedArticleLink>();
    public ICollection<RelatedArticleLink> RelatedTo { get; private set; } = new List<RelatedArticleLink>();

    public bool IsPublished => Status == ArticleStatus.Published;
    public bool IsPubliclyVisible => Status == ArticleStatus.Published && PublishedAt is not null && PublishedAt <= DateTimeOffset.UtcNow;

    private Article()
    {
    }

    public static Article Create(
        string title,
        string slug,
        string excerpt,
        string content,
        Guid authorId,
        Guid categoryId,
        DateTimeOffset now,
        Guid? createdByAgentId = null,
        bool isAiGenerated = false)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("TITLE_REQUIRED", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("SLUG_REQUIRED", "Slug is required.");
        }

        var article = new Article
        {
            Title = title.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Excerpt = excerpt.Trim(),
            Content = content,
            AuthorId = authorId,
            CategoryId = categoryId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByAgentId = createdByAgentId,
            IsAiGenerated = isAiGenerated,
            Status = ArticleStatus.Draft,
            ContentFormat = ContentFormat.Markdown
        };

        article.ReadingTimeMinutes = CalculateReadingTime(content);
        article.ContentHash = ComputeContentHash(content);
        return article;
    }

    public void UpdateEditorial(
        string title,
        string slug,
        string? subtitle,
        string excerpt,
        string content,
        Guid categoryId,
        Difficulty difficulty,
        ArticleContentType contentType,
        Guid? seriesId,
        int? seriesOrder,
        bool isFeatured,
        DateTimeOffset now)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("TITLE_REQUIRED", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("SLUG_REQUIRED", "Slug is required.");
        }

        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();
        Excerpt = excerpt.Trim();
        Content = content;
        CategoryId = categoryId;
        Difficulty = difficulty;
        ContentType = contentType;
        SeriesId = seriesId;
        SeriesOrder = seriesOrder;
        IsFeatured = isFeatured;
        UpdatedAt = now;
        ReadingTimeMinutes = CalculateReadingTime(content);
        ContentHash = ComputeContentHash(content);
    }

    public void UpdateSeo(
        string? metaTitle,
        string? metaDescription,
        string? canonicalUrl,
        string? ogTitle,
        string? ogDescription,
        string? ogImage,
        DateTimeOffset now)
    {
        EnsureEditable();
        MetaTitle = NormalizeOptional(metaTitle);
        MetaDescription = NormalizeOptional(metaDescription);
        CanonicalUrl = NormalizeOptional(canonicalUrl);
        OgTitle = NormalizeOptional(ogTitle);
        OgDescription = NormalizeOptional(ogDescription);
        OgImage = NormalizeOptional(ogImage);
        UpdatedAt = now;
    }

    public void SetImages(Guid? featuredImageId, Guid? thumbnailImageId, DateTimeOffset now)
    {
        EnsureEditable();
        FeaturedImageId = featuredImageId;
        ThumbnailImageId = thumbnailImageId;
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        if (Status is ArticleStatus.Archived)
        {
            throw new DomainException("ARTICLE_ARCHIVED", "Archived articles cannot be published.");
        }

        Status = ArticleStatus.Published;
        PublishedAt = now;
        ScheduledAt = null;
        RejectionReason = null;
        UpdatedAt = now;
    }

    public void Schedule(DateTimeOffset scheduledAt, DateTimeOffset now)
    {
        if (scheduledAt <= now)
        {
            throw new DomainException("SCHEDULE_IN_PAST", "Scheduled time must be in the future.");
        }

        if (Status is ArticleStatus.Archived)
        {
            throw new DomainException("ARTICLE_ARCHIVED", "Archived articles cannot be scheduled.");
        }

        Status = ArticleStatus.Scheduled;
        ScheduledAt = scheduledAt;
        RejectionReason = null;
        UpdatedAt = now;
    }

    public void Unpublish(DateTimeOffset now)
    {
        if (Status != ArticleStatus.Published && Status != ArticleStatus.Scheduled)
        {
            throw new DomainException("INVALID_STATUS", "Only published or scheduled articles can be unpublished.");
        }

        Status = ArticleStatus.Draft;
        ScheduledAt = null;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = ArticleStatus.Archived;
        ScheduledAt = null;
        UpdatedAt = now;
    }

    public void SubmitForReview(DateTimeOffset now)
    {
        if (Status is not (ArticleStatus.Draft or ArticleStatus.Rejected or ArticleStatus.Scheduled))
        {
            throw new DomainException("INVALID_STATUS", "Only draft, rejected, or scheduled articles can be submitted for review.");
        }

        Status = ArticleStatus.PendingReview;
        RejectionReason = null;
        UpdatedAt = now;
    }

    public void SubmitScheduledForReview(DateTimeOffset scheduledAt, DateTimeOffset now)
    {
        if (scheduledAt <= now)
        {
            throw new DomainException("SCHEDULE_IN_PAST", "Scheduled time must be in the future.");
        }

        ScheduledAt = scheduledAt;
        Status = ArticleStatus.PendingReview;
        RejectionReason = null;
        UpdatedAt = now;
    }

    public void Approve(DateTimeOffset now)
    {
        if (Status != ArticleStatus.PendingReview)
        {
            throw new DomainException("INVALID_STATUS", "Only articles pending review can be approved.");
        }

        RejectionReason = null;
        UpdatedAt = now;

        if (ScheduledAt is not null && ScheduledAt > now)
        {
            Status = ArticleStatus.Scheduled;
            return;
        }

        Publish(now);
    }

    public void Reject(string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("REJECTION_REASON_REQUIRED", "A rejection reason is required.");
        }

        Status = ArticleStatus.Rejected;
        RejectionReason = reason.Trim();
        ScheduledAt = null;
        UpdatedAt = now;
    }

    public void SetPreviewToken(string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        PreviewTokenHash = tokenHash;
        PreviewTokenExpiresAt = expiresAt;
        UpdatedAt = now;
    }

    public bool IsPreviewTokenValid(string tokenHash, DateTimeOffset now)
    {
        return PreviewTokenHash is not null
               && PreviewTokenExpiresAt is not null
               && PreviewTokenExpiresAt > now
               && CryptographicEquals(PreviewTokenHash, tokenHash);
    }

    public void IncrementViews() => ViewCount++;

    public void Touch(DateTimeOffset now) => UpdatedAt = now;

    private void EnsureEditable()
    {
        if (Status == ArticleStatus.Archived)
        {
            throw new DomainException("ARTICLE_ARCHIVED", "Archived articles cannot be edited.");
        }
    }

    public static string ComputeContentHash(string content)
    {
        var normalized = (content ?? string.Empty).Replace("\r\n", "\n").Trim();
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    public static int CalculateReadingTime(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 1;
        }

        var words = content.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / (double)DefaultWordsPerMinute));
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool CryptographicEquals(string a, string b)
        => string.Equals(a, b, StringComparison.Ordinal);
}
