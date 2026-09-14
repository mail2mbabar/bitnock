using Blog.Domain.Common;

namespace Blog.Domain.Entities;

public sealed class Category : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<Article> Articles { get; private set; } = new List<Article>();

    private Category()
    {
    }

    public static Category Create(string name, string slug, string? description, int sortOrder, DateTimeOffset now)
    {
        return new Category
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };
    }

    public void Update(string name, string slug, string? description, int sortOrder, bool isActive, DateTimeOffset now)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = now;
    }
}

public sealed class Tag : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<ArticleTag> ArticleTags { get; private set; } = new List<ArticleTag>();

    private Tag()
    {
    }

    public static Tag Create(string name, string slug, string? description, DateTimeOffset now)
    {
        return new Tag
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt = now
        };
    }

    public void Update(string name, string slug, string? description)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}

public sealed class Technology : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    public ICollection<ArticleTechnology> ArticleTechnologies { get; private set; } = new List<ArticleTechnology>();

    private Technology()
    {
    }

    public static Technology Create(string name, string slug)
    {
        return new Technology
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant()
        };
    }
}

public sealed class Series : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? CoverImageId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<Article> Articles { get; private set; } = new List<Article>();
    public MediaAsset? CoverImage { get; private set; }

    private Series()
    {
    }

    public static Series Create(string name, string slug, string? description, Guid? coverImageId, DateTimeOffset now)
    {
        return new Series
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CoverImageId = coverImageId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string slug, string? description, Guid? coverImageId, DateTimeOffset now)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CoverImageId = coverImageId;
        UpdatedAt = now;
    }
}

public sealed class Author : Entity
{
    public Guid? UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Bio { get; private set; } = string.Empty;
    public Guid? AvatarMediaId { get; private set; }
    public string? GitHubUrl { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public string? XUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public MediaAsset? Avatar { get; private set; }
    public ICollection<Article> Articles { get; private set; } = new List<Article>();

    private Author()
    {
    }

    public static Author Create(
        string displayName,
        string slug,
        string bio,
        DateTimeOffset now,
        Guid? userId = null,
        string? gitHubUrl = null,
        string? linkedInUrl = null,
        string? xUrl = null,
        string? websiteUrl = null)
    {
        return new Author
        {
            DisplayName = displayName.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Bio = bio.Trim(),
            UserId = userId,
            GitHubUrl = gitHubUrl,
            LinkedInUrl = linkedInUrl,
            XUrl = xUrl,
            WebsiteUrl = websiteUrl,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string displayName,
        string slug,
        string bio,
        Guid? avatarMediaId,
        string? gitHubUrl,
        string? linkedInUrl,
        string? xUrl,
        string? websiteUrl,
        DateTimeOffset now)
    {
        DisplayName = displayName.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Bio = bio.Trim();
        AvatarMediaId = avatarMediaId;
        GitHubUrl = gitHubUrl;
        LinkedInUrl = linkedInUrl;
        XUrl = xUrl;
        WebsiteUrl = websiteUrl;
        UpdatedAt = now;
    }

    public void AssignUser(Guid userId) => UserId = userId;
}

public sealed class ArticleTag
{
    public Guid ArticleId { get; private set; }
    public Guid TagId { get; private set; }
    public Article Article { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    private ArticleTag()
    {
    }

    public ArticleTag(Guid articleId, Guid tagId)
    {
        ArticleId = articleId;
        TagId = tagId;
    }
}

public sealed class ArticleTechnology
{
    public Guid ArticleId { get; private set; }
    public Guid TechnologyId { get; private set; }
    public Article Article { get; private set; } = null!;
    public Technology Technology { get; private set; } = null!;

    private ArticleTechnology()
    {
    }

    public ArticleTechnology(Guid articleId, Guid technologyId)
    {
        ArticleId = articleId;
        TechnologyId = technologyId;
    }
}

public sealed class RelatedArticleLink
{
    public Guid ArticleId { get; private set; }
    public Guid RelatedArticleId { get; private set; }
    public double Score { get; private set; }
    public Article Article { get; private set; } = null!;
    public Article RelatedArticle { get; private set; } = null!;

    private RelatedArticleLink()
    {
    }

    public RelatedArticleLink(Guid articleId, Guid relatedArticleId, double score)
    {
        ArticleId = articleId;
        RelatedArticleId = relatedArticleId;
        Score = score;
    }
}
