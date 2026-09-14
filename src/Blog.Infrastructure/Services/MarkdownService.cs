using System.Text.RegularExpressions;
using Blog.Application.Articles;
using Blog.Application.Auth;
using Blog.Application.Common;
using Ganss.Xss;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Blog.Infrastructure.Services;

public sealed class MarkdownService : IMarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    private readonly HtmlSanitizer _sanitizer;

    public MarkdownService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.UnionWith(["h2", "h3", "h4", "pre", "code", "table", "thead", "tbody", "tr", "th", "td", "iframe", "span", "figure", "figcaption"]);
        _sanitizer.AllowedAttributes.UnionWith(["class", "id", "href", "src", "alt", "title", "rel", "target", "width", "height", "allow", "allowfullscreen", "frameborder", "loading", "referrerpolicy"]);
        _sanitizer.AllowedSchemes.UnionWith(["https", "http", "mailto"]);
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedAtRules.Clear();
        _sanitizer.RemovingAttribute += (_, args) =>
        {
            if (args.Tag.TagName.Equals("iframe", StringComparison.OrdinalIgnoreCase)
                && args.Attribute.Name.Equals("src", StringComparison.OrdinalIgnoreCase))
            {
                var src = args.Attribute.Value ?? string.Empty;
                if (!IsAllowedEmbed(src))
                {
                    args.Cancel = false;
                }
            }
        };
    }

    public string ToSafeHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var html = Markdown.ToHtml(EditorialText.WithoutAiDashes(markdown), Pipeline);
        return _sanitizer.Sanitize(html);
    }

    public IReadOnlyList<TocItemDto> ExtractTableOfContents(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var document = Markdown.Parse(markdown, Pipeline);
        var items = new List<TocItemDto>();
        foreach (var heading in document.Descendants<HeadingBlock>().Where(h => h.Level is >= 2 and <= 3))
        {
            var text = heading.Inline?.FirstChild is LiteralInline literal
                ? literal.Content.ToString()
                : heading.Inline?.ToString() ?? string.Empty;
            text = EditorialText.WithoutAiDashes(Regex.Replace(text ?? string.Empty, "<.*?>", string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            items.Add(new TocItemDto(heading.Level, text, SlugifyHeading(text)));
        }

        return items;
    }

    public IReadOnlyList<string> ExtractLinks(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var document = Markdown.Parse(markdown, Pipeline);
        return document.Descendants<LinkInline>()
            .Select(l => l.Url ?? string.Empty)
            .Where(u => u.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string SlugifyHeading(string text)
    {
        var slug = Application.Common.Slugifier.From(text);
        return string.IsNullOrWhiteSpace(slug) ? "section" : slug;
    }

    private static bool IsAllowedEmbed(string src)
    {
        return src.StartsWith("https://www.youtube.com/embed/", StringComparison.OrdinalIgnoreCase)
               || src.StartsWith("https://www.youtube-nocookie.com/embed/", StringComparison.OrdinalIgnoreCase);
    }
}
