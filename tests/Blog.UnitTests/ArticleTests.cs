using Blog.Domain.Entities;
using Blog.Domain.Enums;
using FluentAssertions;

namespace Blog.UnitTests;

public class ArticlePublishingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_as_draft()
    {
        var article = NewArticle();
        article.Status.Should().Be(ArticleStatus.Draft);
        article.IsPublished.Should().BeFalse();
        article.ContentHash.Should().NotBeNullOrEmpty();
        article.ReadingTimeMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Publish_is_idempotent_for_already_published_articles()
    {
        var article = NewArticle();
        article.Publish(Now);
        article.Publish(Now.AddMinutes(1));
        article.Status.Should().Be(ArticleStatus.Published);
        article.PublishedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void Schedule_rejects_past_times()
    {
        var article = NewArticle();
        var act = () => article.Schedule(Now.AddMinutes(-5), Now);
        act.Should().Throw<Blog.Domain.Common.DomainException>().Where(e => e.Code == "SCHEDULE_IN_PAST");
    }

    [Fact]
    public void Agent_approval_flow_preserves_schedule()
    {
        var article = NewArticle();
        article.SubmitScheduledForReview(Now.AddHours(8), Now);
        article.Status.Should().Be(ArticleStatus.PendingReview);
        article.Approve(Now);
        article.Status.Should().Be(ArticleStatus.Scheduled);
        article.ScheduledAt.Should().Be(Now.AddHours(8));
    }

    [Fact]
    public void Reject_requires_reason()
    {
        var article = NewArticle();
        var act = () => article.Reject(" ", Now);
        act.Should().Throw<Blog.Domain.Common.DomainException>();
    }

    [Fact]
    public void Archived_articles_cannot_be_published()
    {
        var article = NewArticle();
        article.Archive(Now);
        var act = () => article.Publish(Now);
        act.Should().Throw<Blog.Domain.Common.DomainException>().Where(e => e.Code == "ARTICLE_ARCHIVED");
    }

    [Fact]
    public void Duplicate_content_hash_is_stable()
    {
        var a = Article.ComputeContentHash("Hello\r\nWorld");
        var b = Article.ComputeContentHash("Hello\nWorld");
        a.Should().Be(b);
    }

    private static Article NewArticle()
        => Article.Create("Title", "title", "An excerpt for tests.", new string('a', 500), Guid.NewGuid(), Guid.NewGuid(), Now);
}

public class SlugifierTests
{
    [Theory]
    [InlineData("Clean Architecture", "clean-architecture")]
    [InlineData("ASP.NET Core", "asp-net-core")]
    [InlineData("  Hello---World  ", "hello-world")]
    public void From_normalizes(string input, string expected)
        => Blog.Application.Common.Slugifier.From(input).Should().Be(expected);
}

public class SecretHasherTests
{
    [Fact]
    public void Api_keys_use_prefix_and_never_embed_raw_secret_in_hash_lookup_key()
    {
        var key = Blog.Infrastructure.Security.SecretHasher.GenerateApiKey(out var prefix);
        key.Should().StartWith("nxa_");
        prefix.Should().Be(key[..12]);
        Blog.Infrastructure.Security.SecretHasher.Hash(key).Should().NotBe(key);
    }
}

public class MarkdownSafetyTests
{
    [Fact]
    public void Script_tags_are_stripped()
    {
        var markdown = new Blog.Infrastructure.Services.MarkdownService();
        var html = markdown.ToSafeHtml("Hello <script>alert(1)</script> **world**");
        html.Should().NotContain("<script");
        html.Should().Contain("world");
    }

    [Fact]
    public void Table_of_contents_uses_h2_and_h3()
    {
        var markdown = new Blog.Infrastructure.Services.MarkdownService();
        var toc = markdown.ExtractTableOfContents("# Title\n\n## One\n\n### Nested\n\n## Two");
        toc.Should().HaveCount(3);
        toc[0].Text.Should().Be("One");
        toc[0].Id.Should().Be("one");
    }
}

public class ValidatorTests
{
    [Fact]
    public async Task Upsert_request_requires_title()
    {
        var validator = new Blog.Application.Validation.UpsertArticleRequestValidator();
        var result = await validator.ValidateAsync(new Blog.Application.Articles.UpsertArticleRequest(
            "", null, null, "excerpt", "content", null, "dotnet", null, null, null, null, null, null, null, null, false, false, null, null, null, null, null, null, null));
        result.IsValid.Should().BeFalse();
    }
}
