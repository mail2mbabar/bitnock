using Blog.Application.Media;
using Blog.Application.Options;
using Blog.Domain.Constants;
using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Infrastructure.Identity;
using Blog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Blog.Infrastructure.Seeding;

public sealed class DatabaseSeeder
{
    public const string AuthorSlug = "muhammad-babar";
    public const string AuthorName = "Muhammad Babar";
    public const string AuthorGitHub = "https://github.com/mail2mbabar";
    public const string AuthorLinkedIn = "https://www.linkedin.com/in/dev-mbabar";
    public const string AuthorYouTube = "https://www.youtube.com/@ABiHelpline";
    public const string AuthorBio =
        "Microsoft MVP (Developer Technologies · .NET, Aug 2026 – Aug 2027) and Senior Full Stack .NET Developer based in Manchester. Eleven years shipping C#, ASP.NET Core, Azure, and Angular across ecommerce, healthcare, aviation, and government. I also teach C# and interview prep on ABi Helpline. This site is where I write the long-form notes.";

    private readonly BlogDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole<Guid>> _roles;
    private readonly SeedOptions _seed;
    private readonly SiteOptions _site;
    private readonly IStorageService _storage;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        BlogDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole<Guid>> roles,
        IOptions<SeedOptions> seed,
        IOptions<SiteOptions> site,
        IStorageService storage,
        IHostEnvironment env,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _users = users;
        _roles = roles;
        _seed = seed.Value;
        _site = site.Value;
        _storage = storage;
        _env = env;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.MigrateAsync(cancellationToken);

        foreach (var role in Roles.All)
        {
            if (!await _roles.RoleExistsAsync(role))
            {
                await _roles.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedContentAsync(cancellationToken);

        if (_env.IsDevelopment() && _seed.Enabled)
        {
            await SeedDevAdminAsync(cancellationToken);
        }
    }

    private async Task SeedDevAdminAsync(CancellationToken cancellationToken)
    {
        var author = await _db.Authors.FirstAsync(a => a.Slug == AuthorSlug, cancellationToken);
        var existing = await _users.FindByEmailAsync(_seed.DevAdminEmail);
        if (existing is not null)
        {
            existing.DisplayName = AuthorName;
            existing.AuthorId = author.Id;
            await _users.UpdateAsync(existing);
            if (author.UserId != existing.Id)
            {
                author.AssignUser(existing.Id);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = _seed.DevAdminEmail,
            Email = _seed.DevAdminEmail,
            EmailConfirmed = true,
            DisplayName = AuthorName,
            AuthorId = author.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _users.CreateAsync(user, _seed.DevAdminPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create development admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await _users.AddToRolesAsync(user, [Roles.Admin, Roles.Editor, Roles.Author]);
        author.AssignUser(user.Id);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("DEVELOPMENT ONLY admin created: {Email}. Do not use these credentials in production.", _seed.DevAdminEmail);
    }

    private async Task SeedContentAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var media = await SeedMediaAsync(now, cancellationToken);
        var categories = await EnsureCategoriesAsync(now, cancellationToken);
        var seriesBySlug = await EnsureSeriesAsync(now, media, cancellationToken);
        var author = await EnsureAuthorAsync(now, media, cancellationToken);

        Category Cat(string slug) => categories.First(c => c.Slug == slug);

        var specs = SeedArticles.All;
        var existing = await _db.Articles.ToListAsync(cancellationToken);
        var bySlug = existing.ToDictionary(a => a.Slug, StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var imaged = 0;
        var updated = 0;

        for (var i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            var publishedAt = now.AddDays(-(specs.Count - i)).AddHours(8);
            var seriesId = spec.SeriesSlug is null ? null : seriesBySlug.GetValueOrDefault(spec.SeriesSlug)?.Id;
            var content = SeedArticlesExpand.Apply(spec.Slug, spec.Content);
            if (!bySlug.TryGetValue(spec.Slug, out var article))
            {
                article = Article.Create(spec.Title, spec.Slug, spec.Excerpt, content, author.Id, Cat(spec.CategorySlug).Id, publishedAt, null, false);
                article.UpdateEditorial(spec.Title, spec.Slug, spec.Subtitle, spec.Excerpt, content, Cat(spec.CategorySlug).Id, spec.Difficulty, spec.ContentType, seriesId, spec.SeriesPart, spec.Featured, publishedAt);
                article.UpdateSeo(Clip(spec.Title, 70), Clip(spec.Excerpt, 160), $"/articles/{spec.Slug}", Clip(spec.Title, 120), Clip(spec.Excerpt, 200), null, publishedAt);
                article.Publish(publishedAt);
                _db.Articles.Add(article);
                bySlug[spec.Slug] = article;
                created++;

                foreach (var tag in spec.Tags)
                {
                    var tagSlug = Application.Common.Slugifier.From(tag);
                    var entity = _db.Tags.Local.FirstOrDefault(t => t.Slug == tagSlug)
                                 ?? await _db.Tags.FirstOrDefaultAsync(t => t.Slug == tagSlug, cancellationToken)
                                 ?? _db.Tags.Add(Tag.Create(tag, tagSlug, null, now)).Entity;
                    article.ArticleTags.Add(new ArticleTag(article.Id, entity.Id));
                }

                foreach (var tech in spec.Technologies)
                {
                    var techSlug = Application.Common.Slugifier.From(tech);
                    var entity = _db.Technologies.Local.FirstOrDefault(t => t.Slug == techSlug)
                                 ?? await _db.Technologies.FirstOrDefaultAsync(t => t.Slug == techSlug, cancellationToken)
                                 ?? _db.Technologies.Add(Technology.Create(tech, techSlug)).Entity;
                    article.ArticleTechnologies.Add(new ArticleTechnology(article.Id, entity.Id));
                }
            }
            else if (article.Content.Length < content.Length)
            {
                article.UpdateEditorial(
                    spec.Title,
                    spec.Slug,
                    spec.Subtitle,
                    spec.Excerpt,
                    content,
                    Cat(spec.CategorySlug).Id,
                    spec.Difficulty,
                    spec.ContentType,
                    seriesId ?? article.SeriesId,
                    spec.SeriesPart ?? article.SeriesOrder,
                    spec.Featured || article.IsFeatured,
                    now);
                article.UpdateSeo(Clip(spec.Title, 70), Clip(spec.Excerpt, 160), $"/articles/{spec.Slug}", Clip(spec.Title, 120), Clip(spec.Excerpt, 200), article.OgImage, now);
                updated++;
            }

            if (media.TryGetValue(SeedArticles.ThumbFile(spec.Slug), out var thumb))
            {
                if (article.FeaturedImageId != thumb.Id)
                {
                    article.SetImages(thumb.Id, thumb.Id, now);
                    imaged++;
                }

                article.UpdateSeo(article.MetaTitle, article.MetaDescription, article.CanonicalUrl, article.OgTitle, article.OgDescription, thumb.Url, now);
            }

            if (seriesId is not null && article.SeriesId != seriesId)
            {
                article.UpdateEditorial(
                    spec.Title,
                    spec.Slug,
                    spec.Subtitle,
                    spec.Excerpt,
                    article.Content,
                    article.CategoryId,
                    spec.Difficulty,
                    spec.ContentType,
                    seriesId,
                    spec.SeriesPart,
                    spec.Featured || article.IsFeatured,
                    now);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Content seed complete. Created {Created} articles, updated {Updated} bodies, attached {Imaged} thumbnails.", created, updated, imaged);
    }

    private static string Clip(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        value = value.Trim();
        return value.Length <= max ? value : value[..(max - 1)].TrimEnd() + "…";
    }

    private async Task<Author> EnsureAuthorAsync(DateTimeOffset now, Dictionary<string, MediaAsset> media, CancellationToken cancellationToken)
    {
        var avatarId = media.GetValueOrDefault("muhammad-babar.jpg")?.Id;
        var author = await _db.Authors
            .OrderBy(a => a.CreatedAt)
            .FirstOrDefaultAsync(a => a.Slug == AuthorSlug || a.Slug == "alex-mercer", cancellationToken)
            ?? await _db.Authors.OrderBy(a => a.CreatedAt).FirstOrDefaultAsync(cancellationToken);

        if (author is null)
        {
            author = Author.Create(
                AuthorName,
                AuthorSlug,
                AuthorBio,
                now,
                gitHubUrl: AuthorGitHub,
                linkedInUrl: AuthorLinkedIn,
                websiteUrl: _site.Url);
            author.Update(AuthorName, AuthorSlug, AuthorBio, avatarId, AuthorGitHub, AuthorLinkedIn, null, _site.Url, now);
            _db.Authors.Add(author);
            await _db.SaveChangesAsync(cancellationToken);
            return author;
        }

        author.Update(AuthorName, AuthorSlug, AuthorBio, avatarId ?? author.AvatarMediaId, AuthorGitHub, AuthorLinkedIn, null, _site.Url, now);
        await _db.SaveChangesAsync(cancellationToken);
        return author;
    }

    private async Task<List<Category>> EnsureCategoriesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var wanted = new (string Name, string Slug, string Description, int Order)[]
        {
            (".NET", "dotnet", "The .NET runtime, SDK, and ecosystem.", 1),
            ("ASP.NET Core", "aspnet-core", "Web frameworks, middleware, and hosting.", 2),
            ("C#", "csharp", "Language features and effective C#.", 3),
            ("Entity Framework Core", "ef-core", "Data access, mapping, and performance.", 4),
            ("Architecture", "architecture", "System design for long-lived products.", 5),
            ("Clean Architecture", "clean-architecture", "Boundaries, use cases, and maintainable structure.", 6),
            ("Microservices", "microservices", "Service boundaries and distributed design.", 7),
            ("APIs", "apis", "HTTP APIs, contracts, and versioning.", 8),
            ("Azure", "azure", "Cloud architecture on Microsoft Azure.", 9),
            ("DevOps", "devops", "Delivery pipelines and operational practice.", 10),
            ("Docker", "docker", "Containers and local-to-prod parity.", 11),
            ("Testing", "testing", "Automated tests that earn their keep.", 12),
            ("Performance", "performance", "Latency, allocations, and throughput.", 13),
            ("Security", "security", "Authn, authz, and threat reduction.", 14),
            ("AI", "ai", "AI-assisted engineering, carefully applied.", 15),
            ("Software Engineering", "software-engineering", "Craft, process, and professional practice.", 16),
            ("Comparisons", "comparisons", "Side-by-side choices for .NET teams.", 17),
            ("Interview", "interview", "C#, ASP.NET Core, SQL, and the questions that show up in real interviews.", 18)
        };

        var existing = await _db.Categories.ToListAsync(cancellationToken);
        foreach (var spec in wanted)
        {
            if (existing.Any(c => c.Slug == spec.Slug))
            {
                continue;
            }

            var category = Category.Create(spec.Name, spec.Slug, spec.Description, spec.Order, now);
            _db.Categories.Add(category);
            existing.Add(category);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task<Dictionary<string, Series>> EnsureSeriesAsync(DateTimeOffset now, Dictionary<string, MediaAsset> media, CancellationToken cancellationToken)
    {
        async Task<Series> Ensure(string name, string slug, string description, string? coverFile)
        {
            var series = await _db.Series.FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);
            if (series is null)
            {
                var coverId = coverFile is null ? null : media.GetValueOrDefault(coverFile)?.Id;
                series = Series.Create(name, slug, description, coverId, now);
                _db.Series.Add(series);
            }

            return series;
        }

        var aspnet = await Ensure("Mastering ASP.NET Core", "mastering-aspnet-core", "A practical series on building production ASP.NET Core systems.", "bitnock-building-http-apis-that-age-well-in-aspnet-core.png");
        var azure = await Ensure("Azure for .NET teams", "azure-for-dotnet-teams", "Hosting, messaging, APIs, and delivery choices on Azure, written from production .NET work.", "bitnock-azure-app-service-vs-container-apps-for-a-dotnet-api.png");
        var helpline = await Ensure("ABi Helpline", "abi-helpline", "Written companions to every video on the ABi Helpline YouTube channel — C#, ASP.NET Core, interviews, and architecture.", "bitnock-top-10-csharp-interview-questions-and-answers.png");
        await _db.SaveChangesAsync(cancellationToken);
        return new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase)
        {
            [aspnet.Slug] = aspnet,
            [azure.Slug] = azure,
            [helpline.Slug] = helpline
        };
    }

    private async Task<Dictionary<string, MediaAsset>> SeedMediaAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var root = ResolveMediaRoot();
        var result = new Dictionary<string, MediaAsset>(StringComparer.OrdinalIgnoreCase);
        if (root is null)
        {
            _logger.LogWarning("Seed media folder was not found. Folder uploads will be skipped; titled thumbnails will still be generated.");
            foreach (var asset in await _db.MediaAssets.ToListAsync(cancellationToken))
            {
                result[asset.FileName] = asset;
            }

            await SeedGeneratedThumbsAsync(result, now, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return result;
        }

        foreach (var asset in await _db.MediaAssets.ToListAsync(cancellationToken))
        {
            result[asset.FileName] = asset;
        }

        foreach (var path in Directory.GetFiles(root))
        {
            var fileName = Path.GetFileName(path);
            if (result.TryGetValue(fileName, out var existing)
                && await _storage.ExistsAsync(existing.StorageKey, cancellationToken))
            {
                continue;
            }

            await using var stream = File.OpenRead(path);
            var mime = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "image/png"
            };
            int? width = null;
            int? height = null;
            try
            {
                stream.Position = 0;
                using var image = await Image.LoadAsync(stream, cancellationToken);
                width = image.Width;
                height = image.Height;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read dimensions for {File}", fileName);
            }

            stream.Position = 0;
            var stored = await _storage.SaveAsync(stream, fileName, mime, cancellationToken);
            if (existing is not null)
            {
                existing.ReplaceStored(stored.StorageKey, stored.Url, stored.FileSize);
                continue;
            }

            var (title, alt) = MediaCopy(fileName);
            var entity = MediaAsset.Create(fileName, stored.StorageKey, stored.Url, mime, stored.FileSize, width, height, alt, title, null, now);
            _db.MediaAssets.Add(entity);
            result[fileName] = entity;
        }

        try
        {
            await SeedGeneratedThumbsAsync(result, now, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Skipping generated article thumbnails; seed will continue without them.");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task SeedGeneratedThumbsAsync(Dictionary<string, MediaAsset> result, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var spec in SeedArticles.All)
        {
            var fileName = SeedArticles.ThumbFile(spec.Slug);
            if (result.TryGetValue(fileName, out var existing)
                && await _storage.ExistsAsync(existing.StorageKey, cancellationToken))
            {
                continue;
            }

            var style = SeedArticles.ThumbStyle(spec.Slug);
            await using var stream = CreatorThumbnailGenerator.Render(spec.Title, style.Badge, style.Accent, style.Play);
            var stored = await _storage.SaveAsync(stream, fileName, "image/png", cancellationToken);
            if (existing is not null)
            {
                existing.ReplaceStored(stored.StorageKey, stored.Url, stored.FileSize);
                continue;
            }

            var entity = MediaAsset.Create(
                fileName,
                stored.StorageKey,
                stored.Url,
                "image/png",
                stored.FileSize,
                CreatorThumbnailGenerator.Width,
                CreatorThumbnailGenerator.Height,
                $"{spec.Title} — titled article thumbnail",
                spec.Title,
                spec.Excerpt,
                now);
            _db.MediaAssets.Add(entity);
            result[fileName] = entity;
        }
    }

    private string? ResolveMediaRoot()
    {
        var candidates = new[]
        {
            Path.Combine(_env.ContentRootPath, "seed-media"),
            Path.Combine(AppContext.BaseDirectory, "seed-media"),
            Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "..", "content", "seed-media"))
        };
        return candidates.FirstOrDefault(Directory.Exists);
    }

    private static (string Title, string Alt) MediaCopy(string fileName)
    {
        if (fileName.StartsWith("muhammad-babar", StringComparison.OrdinalIgnoreCase))
        {
            return ("Muhammad Babar", "Muhammad Babar, Microsoft MVP and Senior Full Stack .NET Developer");
        }

        if (fileName.Contains("mvp", StringComparison.OrdinalIgnoreCase))
        {
            return ("Microsoft MVP", "Microsoft Most Valuable Professional badge");
        }

        var key = Path.GetFileNameWithoutExtension(fileName);
        var titled = key.Replace("hero-", "").Replace("media-", "").Replace("thumb-", "").Replace("bitnock-", "").Replace("-", " ");
        titled = string.Join(' ', titled.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        var kind = fileName.StartsWith("thumb-", StringComparison.OrdinalIgnoreCase) || fileName.StartsWith("bitnock-", StringComparison.OrdinalIgnoreCase)
            ? "Titled article thumbnail"
            : fileName.StartsWith("media-", StringComparison.OrdinalIgnoreCase) ? "Article figure" : "Editorial hero";
        return (titled, $"{kind}: {titled}");
    }
}

internal sealed record SeedArticleSpec(
    string Title,
    string Slug,
    string Subtitle,
    string Excerpt,
    string Content,
    string CategorySlug,
    string[] Tags,
    string[] Technologies,
    Difficulty Difficulty,
    ArticleContentType ContentType,
    bool Featured,
    int? SeriesPart,
    string? SeriesSlug = null);

internal static class SeedArticles
{
    public static string ThumbFile(string slug) => $"bitnock-{slug}.png";

    public static (string Badge, string Accent, bool Play) ThumbStyle(string slug)
    {
        var video = AbiHelplineVideos.All.FirstOrDefault(v => v.ArticleSlug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (video is not null)
        {
            return (video.Badge, video.AccentHex, true);
        }

        if (slug.Contains("-vs-", StringComparison.OrdinalIgnoreCase)) return ("VS", "F59E0B", false);
        if (slug.Contains("azure", StringComparison.OrdinalIgnoreCase)) return ("AZURE", "0078D4", false);
        if (slug.Contains("ef-core", StringComparison.OrdinalIgnoreCase) || slug.Contains("dapper", StringComparison.OrdinalIgnoreCase)) return ("DATA", "2563EB", false);
        if (slug.Contains("test", StringComparison.OrdinalIgnoreCase)) return ("TESTING", "CA8A04", false);
        if (slug.Contains("docker", StringComparison.OrdinalIgnoreCase) || slug.Contains("github-actions", StringComparison.OrdinalIgnoreCase)) return ("DEVOPS", "0F766E", false);
        if (slug.Contains("csharp", StringComparison.OrdinalIgnoreCase) || slug.Contains("dotnet", StringComparison.OrdinalIgnoreCase)) return ("C#", "E11D48", false);
        return ("GUIDE", "3B5BDB", false);
    }

    public static string? HeroImage(string slug) => ThumbFile(slug);

    public static List<SeedArticleSpec> All =>
    [
        Create("Building HTTP APIs that age well in ASP.NET Core", "building-http-apis-that-age-well-in-aspnet-core",
            "Contracts, versioning, and the details that keep clients from breaking.",
            "A practical guide to designing ASP.NET Core APIs that remain stable as your product grows.",
            "aspnet-core", ["ASP.NET Core", "REST API", ".NET 10"], ["ASP.NET Core", "C#"], Difficulty.Intermediate, ArticleContentType.Guide, true, 1, ApiArticle),
        Create("Modern C# patterns I actually use in production", "modern-csharp-patterns-i-actually-use-in-production",
            "Records, pattern matching, and the features that reduce noise rather than add it.",
            "A field guide to C# language features that improve correctness without turning every file into a showcase.",
            "csharp", ["C#", ".NET 10"], ["C#"], Difficulty.Intermediate, ArticleContentType.DeepDive, false, null, CsharpArticle),
        Create("Entity Framework Core performance: the 80/20 checklist", "entity-framework-core-performance-the-80-20-checklist",
            "N+1 queries, projections, and the indexes your profiler is already hinting at.",
            "How to make EF Core fast enough for production traffic without abandoning the ORM.",
            "ef-core", ["EF Core", "PostgreSQL", "Performance"], ["EF Core", "PostgreSQL"], Difficulty.Advanced, ArticleContentType.Guide, true, null, EfArticle),
        Create("Clean Architecture in ASP.NET Core without the ceremony", "clean-architecture-in-aspnet-core-without-the-ceremony",
            "Boundaries that protect your domain, not folders that impress a diagram.",
            "A pragmatic Clean Architecture layout for ASP.NET Core that stays navigable after year two.",
            "clean-architecture", ["Clean Architecture", "ASP.NET Core"], ["ASP.NET Core", "C#"], Difficulty.Intermediate, ArticleContentType.Architecture, true, 2, CleanArticle),
        Create("API versioning that clients can live with", "api-versioning-that-clients-can-live-with",
            "URL versions, headers, and the social contract behind a breaking change.",
            "How to version ASP.NET Core APIs so both humans and machines can evolve safely.",
            "apis", ["REST API", "ASP.NET Core"], ["ASP.NET Core"], Difficulty.Intermediate, ArticleContentType.Guide, false, 3, VersioningArticle),
        Create("Dependency injection in ASP.NET Core: lifetimes, pitfalls, and tests", "dependency-injection-in-aspnet-core-lifetimes-pitfalls-and-tests",
            "Singleton capturing a scoped service is still the bug that ships.",
            "Understand ASP.NET Core DI lifetimes well enough to design services that are testable and safe.",
            "aspnet-core", ["ASP.NET Core", "Testing"], ["ASP.NET Core", "C#"], Difficulty.Beginner, ArticleContentType.Tutorial, false, 4, DiArticle),
        Create("Dockerizing a .NET API for local development and CI", "dockerizing-a-dotnet-api-for-local-development-and-ci",
            "Multi-stage builds, non-root users, and compose files that new teammates can actually run.",
            "A production-minded Dockerfile and compose setup for ASP.NET Core and PostgreSQL.",
            "docker", ["Docker", "DevOps"], ["Docker", "ASP.NET Core"], Difficulty.Intermediate, ArticleContentType.Tutorial, false, null, DockerArticle),
        Create("Azure App Service vs Container Apps for a .NET API", "azure-app-service-vs-container-apps-for-a-dotnet-api",
            "Choose the hosting model that matches your operational maturity, not the conference talk.",
            "A comparison of Azure hosting options for ASP.NET Core APIs with an emphasis on operations.",
            "azure", ["Azure", "DevOps", "Comparisons"], ["Azure", "ASP.NET Core"], Difficulty.Intermediate, ArticleContentType.Opinion, false, 1, AzureArticle, "azure-for-dotnet-teams"),
        Create("Measuring ASP.NET Core performance before you rewrite anything", "measuring-aspnet-core-performance-before-you-rewrite-anything",
            "BenchmarkDotNet, EventCounters, and the graphs that stop premature optimization.",
            "How to establish a performance baseline for an ASP.NET Core service and act on evidence.",
            "performance", ["Performance", "ASP.NET Core"], ["ASP.NET Core", "C#"], Difficulty.Advanced, ArticleContentType.DeepDive, false, null, PerfArticle),
        Create("Testing ASP.NET Core: what belongs in a unit test vs an API test", "testing-aspnet-core-what-belongs-in-a-unit-test-vs-an-api-test",
            "WebApplicationFactory is not a personality. Use it where it earns its keep.",
            "A testing strategy for ASP.NET Core that keeps unit tests fast and integration tests meaningful.",
            "testing", ["Testing", "ASP.NET Core"], ["ASP.NET Core", "xUnit"], Difficulty.Intermediate, ArticleContentType.Guide, false, null, TestArticle),
        .. SeedArticlesMore.Items,
        .. SeedArticlesVideos.Items
    ];

    internal static SeedArticleSpec Create(
        string title, string slug, string subtitle, string excerpt, string category, string[] tags, string[] techs,
        Difficulty difficulty, ArticleContentType type, bool featured, int? part, string content, string? seriesSlug = null)
        => new(title, slug, subtitle, excerpt, content, category, tags, techs, difficulty, type, featured, part, seriesSlug ?? (part is null ? null : "mastering-aspnet-core"));

    private const string ApiArticle = """
## Start with the contract

An ASP.NET Core API lives or dies by the stability of its contract. Frameworks change. Hosting changes. The JSON your clients parse should change only when you mean it.

I model public payloads as records in an application layer, not as EF entities:

```csharp
public sealed record CreateArticleRequest(
    string Title,
    string Excerpt,
    string Content,
    Guid CategoryId);
```

Controllers stay thin. They authenticate, bind, and call a service. They do not know how an article is stored.

## Version from day one

Even a v1-only product should live under `/api/v1`. The cost is a prefix. The benefit is that a future `/api/v2` does not require an identity crisis.

```http
POST /api/v1/articles HTTP/1.1
Content-Type: application/json
Idempotency-Key: 8f2c1a0e-4b77-4d9a-9c11-2b8f0d1e9aa2
```

Idempotency is not optional if an AI agent or a mobile client can retry. Store the key, the request hash, and the response. Return the original result when the same key arrives again.

## Errors should be machine-readable

Humans can parse a paragraph. Agents cannot. Return a stable code:

```json
{
  "success": false,
  "error": {
    "code": "ARTICLE_NOT_FOUND",
    "message": "Article was not found."
  },
  "requestId": "00-abc"
}
```

Keep HTTP status codes honest: 401 for missing credentials, 403 for missing permission, 409 for conflict, 422 for validation.

## What to postpone

You do not need GraphQL, gRPC, and three gateways on day one. You need a boring REST surface, OpenAPI that matches reality, and tests around publishing transitions. That is the API clients — human or otherwise — can automate.
""";

    private const string CsharpArticle = """
## Prefer boring C# that fails loudly

Modern C# is full of features. I use the ones that remove nulls, copies, and switch-statement rot.

Records are the default for DTOs. `required` properties catch missing JSON. Pattern matching replaces nested `if` trees when the shapes are real:

```csharp
return article.Status switch
{
    ArticleStatus.Draft => "Ready for editing",
    ArticleStatus.PendingReview => "Waiting on an editor",
    ArticleStatus.Scheduled => $"Publishes {article.ScheduledAt:u}",
    ArticleStatus.Published => "Live",
    ArticleStatus.Rejected => article.RejectionReason ?? "Rejected",
    _ => "Archived"
};
```

## Nullability is a contract

Enable nullable reference types and treat warnings as the compiler telling you the truth. A `string?` in a domain method is a design decision, not a leftover.

```csharp
public void Reject(string reason, DateTimeOffset now)
{
    if (string.IsNullOrWhiteSpace(reason))
    {
        throw new DomainException("REJECTION_REASON_REQUIRED", "A rejection reason is required.");
    }

    Status = ArticleStatus.Rejected;
    RejectionReason = reason.Trim();
    UpdatedAt = now;
}
```

## Collection expressions and spans, selectively

Collection expressions are fine for constants and small allocations. `Span<T>` belongs in parsers and hot paths, not in every service method. The point of modern C# is clarity, not density.

If a feature needs a comment to explain why it exists, I usually delete the feature.
""";

    private const string EfArticle = """
## The profiler is cheaper than a rewrite

Most EF Core issues I see in production are not "the ORM is slow". They are unbounded includes, hidden N+1 queries, and `ToList` before a projection.

```csharp
var articles = await db.Articles
    .AsNoTracking()
    .Where(a => a.Status == ArticleStatus.Published)
    .Select(a => new ArticleListItemDto(
        a.Id, a.Title, a.Slug, a.Excerpt, a.Status.ToString(),
        a.PublishedAt, a.CreatedAt, a.UpdatedAt, a.ReadingTimeMinutes,
        a.ViewCount, a.IsFeatured, a.IsAiGenerated,
        a.Category.Name, a.Category.Slug, a.Author.DisplayName, a.Author.Slug,
        a.FeaturedImage != null ? a.FeaturedImage.Url : null,
        a.ArticleTags.Select(t => t.Tag.Name).ToList(),
        a.ArticleTechnologies.Select(t => t.Technology.Name).ToList(),
        a.Difficulty.ToString(), a.ContentType.ToString()))
    .ToListAsync(cancellationToken);
```

Project what the page needs. Leave the markdown body out of list endpoints.

## Indexes you actually need

Slug lookups, status + published date, and foreign keys are not optional:

```sql
CREATE UNIQUE INDEX ix_articles_slug ON articles (slug);
CREATE INDEX ix_articles_status_published ON articles (status, published_at DESC);
```

If a query filters on `tag.slug`, make sure the join table is indexed on both sides. EF will create the PK, but your list pages still need covering indexes as traffic grows.

## Transactions around publishing

Publishing is a state change plus an audit row. Do both in one transaction so a crash cannot leave a live article without a record of who published it.
""";

    private const string CleanArticle = """
## Architecture is a dependency rule

Clean Architecture is not a folder template. It is the rule that inner layers do not mention outer ones. Domain does not know EF. Application does not know HTTP. Infrastructure implements ports.

A useful first cut for a publishing platform:

```text
Blog.Domain          entities and invariants
Blog.Application     use cases, DTOs, validation
Blog.Infrastructure  EF Core, storage, email, search
Blog.Api             auth, controllers, middleware
```

If a controller starts calculating reading time, the boundary already leaked.

## Resist the urge to abstract everything

I do not introduce a repository per table. `BlogDbContext` is the unit of work. Application services that need persistence live in Infrastructure and implement application interfaces. That is enough until a second database exists.

## Where AI agents fit

An agent is another client. It should not live inside the domain. Give it a versioned API, scopes, and audit. Keep the publishing invariants in `Article.Publish`, not in a prompt.

The payoff shows up later: you can replace local search with OpenSearch, or local disk with R2, without rewriting the article state machine.
""";

    private const string VersioningArticle = """
## Version the surface, not every type

URL versioning (`/api/v1`) is the least surprising option for both browsers and agents. Header versioning is fine internally. I avoid mixing both.

When v2 exists, v1 stays until measured traffic dies. Sunset headers are politeness. Tests are insurance.

```csharp
app.MapControllerRoute(
    name: "v1",
    pattern: "api/v1/{controller}/{action?}");
```

## Additive change is free; semantic change is not

Adding a field is usually compatible. Renaming `excerpt` to `summary` is a new version. Returning HTML where Markdown was promised is a new version.

Document the policy in OpenAPI so an agent can read it:

```yaml
/api/v1/agent/publishing-rules:
  get:
    summary: Machine-readable publishing constraints
```

If the rules endpoint says Markdown only, do not silently accept HTML.

## Deprecation is a product feature

Log v1 usage. Put a date on it. Tell clients in the payload if you must, but never surprise them with a 404 on a path that shipped.
""";

    private const string DiArticle = """
## Three lifetimes, three failure modes

- **Transient**: new instance every time. Good for lightweight, stateless helpers.
- **Scoped**: one per request. Default for DbContext and user-facing services.
- **Singleton**: one per process. Safe only if the service is thread-safe and does not capture scoped dependencies.

The classic bug:

```csharp
public sealed class CachedTaxonomy(BlogDbContext db) // captured DbContext
{
}
```

A singleton that takes `BlogDbContext` will use a disposed context. The fix is an `IServiceScopeFactory` or making the cache a scoped decorator.

## Prefer constructor injection and options

```csharp
public sealed class ArticleService(
    BlogDbContext db,
    IOptions<PublishingOptions> publishing)
{
}
```

Do not read `IConfiguration` in the domain of a method. Bind options, validate on startup, fail fast if the JWT signing key is missing.

## Tests become easy when lifetimes are honest

If a service needs the clock, inject `TimeProvider`. Scheduled publishing tests should not wait for 08:00 UTC. They should advance a `FakeTimeProvider`.
""";

    private const string DockerArticle = """
## Multi-stage is the default

A usable ASP.NET Core Dockerfile is two stages: restore/publish, then a slim runtime image.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Blog.Api/Blog.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
ENTRYPOINT ["dotnet", "Blog.Api.dll"]
```

## Compose should boot the platform

Local development should not require a tribal README paragraph. PostgreSQL, API, and web should start together:

```yaml
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_PASSWORD: nexus
  api:
    build: .
    depends_on: [db]
```

Mount storage as a volume. Never bake secrets into the image. Pass connection strings as environment variables.

## Health checks are part of the container contract

`/health/ready` should fail until migrations have run and the database accepts connections. Orchestrators will thank you by not sending traffic to a booting process.
""";

    private const string AzureArticle = """
## App Service is still a good default

If you have one API, a PostgreSQL Flexible Server, and a modest team, Azure App Service plus a container registry is operationally cheap. You get HTTPS, scaling, and a deployment center without inventing a platform.

Container Apps (or AKS) start to win when you have many services, background workers with bursty load, or a real need for scale-to-zero.

## Decide with operations, not fashion

Ask:

1. Who gets paged?
2. How do we roll back?
3. Where do logs go?
4. What happens to scheduled publishing if the process restarts?

A background worker that only uses in-memory timers will miss scheduled posts after a swap. Persist the schedule in PostgreSQL. Poll it. Survive restarts.

## Identity and secrets

Managed identity to Key Vault beats connection strings in App Settings. If you must start with App Settings, at least rotate the JWT signing key independently of the database password.
""";

    private const string PerfArticle = """
## Measure the request path first

Before rewriting JSON serializers, look at the server timing of the article page. Most of the cost is data:

- over-fetching markdown for list views
- missing cache headers on public GET
- images without dimensions
- fonts that block rendering

ASP.NET Core output caching on public article JSON is a one-day win:

```csharp
builder.Services.AddOutputCache(o =>
{
    o.AddBasePolicy(p => p.Expire(TimeSpan.FromSeconds(30)));
});
```

Invalidate on publish. Do not cache authenticated admin responses.

## Allocations matter after correctness

Use `EventCounters` and `dotnet-counters`. If gen2 collections spike on the article list, you are materializing too much. If they do not, stop chasing allocations.

BenchmarkDotNet is for libraries and parsers, not for "is my controller fast". Load tests with a realistic corpus of articles will tell you more.

## Core Web Vitals are a backend problem too

TTFB is your API plus your database. A 2 MB HTML document with inline CSS is still faster than a client-rendered article waiting on five waterfalls. Prefer server rendering for the reading experience.
""";

    private const string TestArticle = """
## Unit tests own the state machine

`Article.Publish` should be tested without a database. If the domain cannot be tested in isolation, it is not a domain — it is a pile of setters.

```csharp
[Fact]
public void Publish_sets_status_and_timestamp()
{
    var article = Article.Create("Title", "title", "Excerpt", new string('a', 500), Guid.NewGuid(), Guid.NewGuid(), Time);
    article.Publish(Time);
    article.Status.Should().Be(ArticleStatus.Published);
}
```

## Integration tests own HTTP and auth

Use `WebApplicationFactory` for login, agent keys, idempotency, and publishing. Swap PostgreSQL for Testcontainers in CI so you are not testing a fantasy provider.

```csharp
public class AgentApiTests : IClassFixture<BlogApiFactory>
{
    [Fact]
    public async Task Agent_cannot_create_admin_users()
    {
        var client = _factory.CreateAgentClient(scopes: ["articles.create"]);
        var response = await client.GetAsync("/api/v1/admin/agents");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

## Skip tests that do not change your confidence

A test that asserts a mapper copied `Title` to `Title` is noise. A test that proves a retried `Idempotency-Key` does not publish twice is a reason to keep the suite.
""";
}
