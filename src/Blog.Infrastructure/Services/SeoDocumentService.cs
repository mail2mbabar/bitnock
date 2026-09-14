using System.Text;
using System.Xml.Linq;
using Blog.Application.Auth;
using Blog.Application.Options;
using Blog.Domain.Enums;
using Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Blog.Infrastructure.Services;

public sealed class SeoDocumentService : ISeoDocumentService
{
    private readonly BlogDbContext _db;
    private readonly SiteOptions _site;

    public SeoDocumentService(BlogDbContext db, IOptions<SiteOptions> site)
    {
        _db = db;
        _site = site.Value;
    }

    public async Task<string> GetSitemapAsync(CancellationToken cancellationToken)
    {
        var origin = _site.Url.TrimEnd('/');
        var urls = new List<(string Loc, DateTimeOffset LastMod, string Freq, string Priority)>
        {
            ($"{origin}/", DateTimeOffset.UtcNow, "daily", "1.0"),
            ($"{origin}/articles", DateTimeOffset.UtcNow, "hourly", "0.9"),
            ($"{origin}/about", DateTimeOffset.UtcNow, "monthly", "0.4")
        };

        var articles = await _db.Articles.AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published)
            .Select(a => new { a.Slug, a.UpdatedAt })
            .ToListAsync(cancellationToken);
        urls.AddRange(articles.Select(a => ($"{origin}/articles/{a.Slug}", a.UpdatedAt, "weekly", "0.8")));

        var categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive).Select(c => new { c.Slug, c.UpdatedAt }).ToListAsync(cancellationToken);
        urls.AddRange(categories.Select(c => ($"{origin}/categories/{c.Slug}", c.UpdatedAt, "weekly", "0.6")));

        var tags = await _db.Tags.AsNoTracking().Select(t => new { t.Slug, t.CreatedAt }).ToListAsync(cancellationToken);
        urls.AddRange(tags.Select(t => ($"{origin}/tags/{t.Slug}", t.CreatedAt, "weekly", "0.5")));

        var series = await _db.Series.AsNoTracking().Select(s => new { s.Slug, s.UpdatedAt }).ToListAsync(cancellationToken);
        urls.AddRange(series.Select(s => ($"{origin}/series/{s.Slug}", s.UpdatedAt, "weekly", "0.6")));

        var authors = await _db.Authors.AsNoTracking().Select(a => new { a.Slug, a.UpdatedAt }).ToListAsync(cancellationToken);
        urls.AddRange(authors.Select(a => ($"{origin}/author/{a.Slug}", a.UpdatedAt, "weekly", "0.5")));

        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");
        foreach (var url in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(url.Loc)}</loc>");
            sb.AppendLine($"    <lastmod>{url.LastMod:yyyy-MM-dd}</lastmod>");
            sb.AppendLine($"    <changefreq>{url.Freq}</changefreq>");
            sb.AppendLine($"    <priority>{url.Priority}</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    public async Task<string> GetRssAsync(CancellationToken cancellationToken)
    {
        var origin = _site.Url.TrimEnd('/');
        var articles = await _db.Articles.AsNoTracking()
            .Include(a => a.Author)
            .Where(a => a.Status == ArticleStatus.Published)
            .OrderByDescending(a => a.PublishedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XElement("channel",
                    new XElement("title", _site.Name),
                    new XElement("link", origin),
                    new XElement("description", _site.Description),
                    new XElement("language", "en-us"),
                    articles.Select(a => new XElement("item",
                        new XElement("title", a.Title),
                        new XElement("link", $"{origin}/articles/{a.Slug}"),
                        new XElement("guid", $"{origin}/articles/{a.Slug}"),
                        new XElement("pubDate", (a.PublishedAt ?? a.CreatedAt).ToString("r")),
                        new XElement("author", a.Author.DisplayName),
                        new XElement("description", new XCData(a.Excerpt))
                    ))
                )));
        return feed.ToString();
    }

    public string GetRobots()
    {
        var origin = _site.Url.TrimEnd('/');
        return $"""
                User-agent: *
                Allow: /
                Disallow: /admin
                Disallow: /admin/
                Disallow: /private
                Disallow: /preview
                Disallow: /api/
                Disallow: /login

                Sitemap: {origin}/sitemap.xml
                """;
    }
}
