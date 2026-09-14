using Blog.Domain.Entities;
using Blog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Persistence;

public sealed class BlogDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<AgentCredential> AgentCredentials => Set<AgentCredential>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<ArticleTechnology> ArticleTechnologies => Set<ArticleTechnology>();
    public DbSet<RelatedArticleLink> RelatedArticles => Set<RelatedArticleLink>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Article>(entity =>
        {
            entity.ToTable("articles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Subtitle).HasMaxLength(240);
            entity.Property(x => x.Excerpt).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CanonicalUrl).HasMaxLength(500);
            entity.Property(x => x.MetaTitle).HasMaxLength(70);
            entity.Property(x => x.MetaDescription).HasMaxLength(180);
            entity.Property(x => x.OgTitle).HasMaxLength(120);
            entity.Property(x => x.OgDescription).HasMaxLength(200);
            entity.Property(x => x.OgImage).HasMaxLength(500);
            entity.Property(x => x.ContentHash).HasMaxLength(64);
            entity.Property(x => x.RejectionReason).HasMaxLength(1000);
            entity.Property(x => x.PreviewTokenHash).HasMaxLength(128);
            entity.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.PublishedAt);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.UpdatedAt);
            entity.HasIndex(x => x.ContentHash);
            entity.HasIndex(x => x.IsFeatured);
            entity.HasOne(x => x.Author).WithMany(x => x.Articles).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany(x => x.Articles).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Series).WithMany(x => x.Articles).HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.FeaturedImage).WithMany().HasForeignKey(x => x.FeaturedImageId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ThumbnailImage).WithMany().HasForeignKey(x => x.ThumbnailImageId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(400);
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Technology>(entity =>
        {
            entity.ToTable("technologies");
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Series>(entity =>
        {
            entity.ToTable("series");
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasOne(x => x.CoverImage).WithMany().HasForeignKey(x => x.CoverImageId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Author>(entity =>
        {
            entity.ToTable("authors");
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Bio).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasOne(x => x.Avatar).WithMany().HasForeignKey(x => x.AvatarMediaId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ArticleTag>(entity =>
        {
            entity.ToTable("article_tags");
            entity.HasKey(x => new { x.ArticleId, x.TagId });
            entity.HasOne(x => x.Article).WithMany(x => x.ArticleTags).HasForeignKey(x => x.ArticleId);
            entity.HasOne(x => x.Tag).WithMany(x => x.ArticleTags).HasForeignKey(x => x.TagId);
        });

        builder.Entity<ArticleTechnology>(entity =>
        {
            entity.ToTable("article_technologies");
            entity.HasKey(x => new { x.ArticleId, x.TechnologyId });
            entity.HasOne(x => x.Article).WithMany(x => x.ArticleTechnologies).HasForeignKey(x => x.ArticleId);
            entity.HasOne(x => x.Technology).WithMany(x => x.ArticleTechnologies).HasForeignKey(x => x.TechnologyId);
        });

        builder.Entity<RelatedArticleLink>(entity =>
        {
            entity.ToTable("related_articles");
            entity.HasKey(x => new { x.ArticleId, x.RelatedArticleId });
            entity.HasOne(x => x.Article).WithMany(x => x.RelatedFrom).HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.RelatedArticle).WithMany(x => x.RelatedTo).HasForeignKey(x => x.RelatedArticleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MediaAsset>(entity =>
        {
            entity.ToTable("media_assets");
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(400).IsRequired();
            entity.Property(x => x.Url).HasMaxLength(500).IsRequired();
            entity.Property(x => x.MimeType).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.FileName);
        });

        builder.Entity<AgentCredential>(entity =>
        {
            entity.ToTable("agent_credentials");
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.KeyPrefix).HasMaxLength(16).IsRequired();
            entity.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Scopes).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => x.KeyPrefix).IsUnique();
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.Property(x => x.ActorId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ActorName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ResourceType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ResourceId).HasMaxLength(80);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.RequestId).HasMaxLength(80);
            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.Action);
            entity.HasIndex(x => x.ActorId);
        });

        builder.Entity<NewsletterSubscriber>(entity =>
        {
            entity.ToTable("newsletter_subscribers");
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ConfirmTokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        builder.Entity<AnalyticsEvent>(entity =>
        {
            entity.ToTable("analytics_events");
            entity.Property(x => x.EventType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Path).HasMaxLength(400).IsRequired();
            entity.Property(x => x.Referrer).HasMaxLength(500);
            entity.Property(x => x.DeviceCategory).HasMaxLength(40);
            entity.Property(x => x.Country).HasMaxLength(8);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.ArticleId);
        });

        builder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Method).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Path).HasMaxLength(400).IsRequired();
            entity.HasIndex(x => new { x.AgentId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => x.ExpiresAt);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash);
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<SiteSetting>(entity =>
        {
            entity.ToTable("site_settings");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(80);
            entity.Property(x => x.Value).HasMaxLength(4000).IsRequired();
        });
    }
}
