using Blog.Application.Articles;
using Blog.Application.Common;

// PublishingRulesDto is defined in the Articles namespace.

namespace Blog.Application.Auth;

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserProfileDto User);
public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    Guid? AuthorId);

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(string email, string password, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ISearchService
{
    Task<PagedResult<ArticleListItemDto>> SearchAsync(string query, ArticleQuery filters, CancellationToken cancellationToken);
}

public sealed record NewsletterSubscribeRequest(string Email);
public sealed record NewsletterConfirmRequest(string Token);

public interface INewsletterService
{
    Task SubscribeAsync(string email, CancellationToken cancellationToken);
    Task ConfirmAsync(string token, CancellationToken cancellationToken);
    Task UnsubscribeAsync(string email, CancellationToken cancellationToken);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken);
}

public sealed record TrackPageViewRequest(
    string Path,
    Guid? ArticleId,
    string? Referrer,
    string? DeviceCategory,
    string? Country);

public sealed record AnalyticsSummaryDto(
    int TotalArticles,
    int Published,
    int Drafts,
    int Scheduled,
    int PendingReview,
    int AiGenerated,
    long TotalViews,
    IReadOnlyList<ArticleListItemDto> TopArticles,
    IReadOnlyList<ArticleListItemDto> RecentArticles,
    IReadOnlyList<CalendarItemDto> Upcoming,
    IReadOnlyList<DailyMetricDto> ViewsByDay);

public sealed record CalendarItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Status,
    DateTimeOffset? At);

public sealed record DailyMetricDto(DateOnly Date, long Views);

public interface IAnalyticsService
{
    Task TrackPageViewAsync(TrackPageViewRequest request, CancellationToken cancellationToken);
    Task<AnalyticsSummaryDto> GetDashboardAsync(CancellationToken cancellationToken);
}

public sealed record SiteSettingsDto(
    string SiteName,
    string SiteDescription,
    string SiteUrl,
    string LogoUrl,
    string FaviconUrl,
    string DefaultAuthorSlug,
    string PublishingMode,
    SocialPublicDto Social);

public sealed record SocialPublicDto(string? GitHub, string? LinkedIn, string? X, string? YouTube);

public interface ISettingsService
{
    Task<SiteSettingsDto> GetPublicAsync(CancellationToken cancellationToken);
    Task<PublishingRulesDto> GetPublishingRulesAsync(CancellationToken cancellationToken);
    Task UpdatePublishingModeAsync(string mode, CancellationToken cancellationToken);
}

public interface IMarkdownService
{
    string ToSafeHtml(string markdown);
    IReadOnlyList<TocItemDto> ExtractTableOfContents(string markdown);
    IReadOnlyList<string> ExtractLinks(string markdown);
}

public interface ISeoDocumentService
{
    Task<string> GetSitemapAsync(CancellationToken cancellationToken);
    Task<string> GetRssAsync(CancellationToken cancellationToken);
    string GetRobots();
}

public sealed record ImportMarkdownResult(Guid ArticleId, string Title, string Slug);
