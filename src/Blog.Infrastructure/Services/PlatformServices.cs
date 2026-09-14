using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Blog.Application.Agents;
using Blog.Application.Articles;
using Blog.Application.Auth;
using Blog.Application.Common;
using Blog.Application.Options;
using Blog.Domain.Constants;
using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Infrastructure.Identity;
using Blog.Infrastructure.Mapping;
using Blog.Infrastructure.Persistence;
using Blog.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Blog.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly BlogDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly IAuditService _audit;
    private readonly TimeProvider _clock;

    public AuthService(
        UserManager<ApplicationUser> users,
        BlogDbContext db,
        IOptions<JwtOptions> jwt,
        IAuditService audit,
        TimeProvider clock)
    {
        _users = users;
        _db = db;
        _jwt = jwt.Value;
        _audit = audit;
        _clock = clock;
    }

    public async Task<AuthResponse> LoginAsync(string email, string password, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(email);
        var actor = new ActorContext
        {
            ActorId = user?.Id.ToString() ?? email,
            ActorType = ActorType.User,
            ActorName = email,
            IpAddress = ipAddress
        };

        if (user is null || !await _users.CheckPasswordAsync(user, password))
        {
            await _audit.RecordAsync(actor, AuditActions.LoginFailed, "auth", user?.Id.ToString(), new { email }, cancellationToken);
            throw AppException.Unauthorized("Invalid email or password.");
        }

        user.LastLoginAt = _clock.GetUtcNow();
        await _users.UpdateAsync(user);
        await _audit.RecordAsync(new ActorContext
        {
            ActorId = actor.ActorId,
            ActorType = actor.ActorType,
            ActorName = user.DisplayName,
            UserId = user.Id,
            IpAddress = actor.IpAddress,
            RequestId = actor.RequestId
        }, AuditActions.LoginSucceeded, "auth", user.Id.ToString(), null, cancellationToken);
        return await IssueAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = SecretHasher.Hash(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
                     ?? throw AppException.Unauthorized("Refresh token is invalid.");
        if (!stored.IsActive(_clock.GetUtcNow()))
        {
            throw AppException.Unauthorized("Refresh token is expired.");
        }

        stored.Revoke(_clock.GetUtcNow());
        var user = await _users.FindByIdAsync(stored.UserId.ToString()) ?? throw AppException.Unauthorized();
        return await IssueAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = SecretHasher.Hash(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        stored?.Revoke(_clock.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await _users.GetRolesAsync(user);
        return new UserProfileDto(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.ToList(), user.AuthorId);
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _users.GetRolesAsync(user);
        var now = _clock.GetUtcNow();
        var expires = now.AddMinutes(_jwt.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new("author_id", user.AuthorId?.ToString() ?? string.Empty)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var access = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = SecretHasher.GenerateToken(48);
        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, SecretHasher.Hash(refresh), now, TimeSpan.FromDays(_jwt.RefreshTokenDays)));
        await _db.SaveChangesAsync(cancellationToken);
        var profile = new UserProfileDto(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.ToList(), user.AuthorId);
        return new AuthResponse(access, refresh, expires, profile);
    }
}

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly BlogDbContext _db;
    private readonly TimeProvider _clock;

    public AnalyticsService(BlogDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task TrackPageViewAsync(TrackPageViewRequest request, CancellationToken cancellationToken)
    {
        _db.AnalyticsEvents.Add(AnalyticsEvent.PageView(
            request.Path,
            request.ArticleId,
            Truncate(request.Referrer, 500),
            request.DeviceCategory,
            request.Country,
            _clock.GetUtcNow()));
        await _db.SaveChangesAsync(cancellationToken);
        if (request.ArticleId is not null)
        {
            var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == request.ArticleId, cancellationToken);
            article?.IncrementViews();
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AnalyticsSummaryDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var articles = _db.Articles.AsNoTracking();
        var total = await articles.CountAsync(cancellationToken);
        var published = await articles.CountAsync(a => a.Status == ArticleStatus.Published, cancellationToken);
        var drafts = await articles.CountAsync(a => a.Status == ArticleStatus.Draft, cancellationToken);
        var scheduled = await articles.CountAsync(a => a.Status == ArticleStatus.Scheduled, cancellationToken);
        var pending = await articles.CountAsync(a => a.Status == ArticleStatus.PendingReview, cancellationToken);
        var ai = await articles.CountAsync(a => a.IsAiGenerated, cancellationToken);
        var views = await articles.SumAsync(a => a.ViewCount, cancellationToken);

        var top = await IncludeList(articles)
            .Where(a => a.Status == ArticleStatus.Published)
            .OrderByDescending(a => a.ViewCount)
            .Take(5)
            .ToListAsync(cancellationToken);
        var recent = await IncludeList(articles)
            .OrderByDescending(a => a.UpdatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);
        var upcoming = await _db.Articles.AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Scheduled || a.Status == ArticleStatus.PendingReview)
            .OrderBy(a => a.ScheduledAt ?? a.UpdatedAt)
            .Take(10)
            .Select(a => new CalendarItemDto(a.Id, a.Title, a.Slug, a.Status.ToString(), a.ScheduledAt ?? a.UpdatedAt))
            .ToListAsync(cancellationToken);

        var since = _clock.GetUtcNow().AddDays(-14);
        var raw = await _db.AnalyticsEvents.AsNoTracking()
            .Where(e => e.CreatedAt >= since)
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken);
        var viewsByDay = raw.Select(x => new DailyMetricDto(DateOnly.FromDateTime(x.Day), x.Count)).OrderBy(x => x.Date).ToList();

        return new AnalyticsSummaryDto(
            total, published, drafts, scheduled, pending, ai, views,
            top.Select(ArticleMapper.ToListItem).ToList(),
            recent.Select(ArticleMapper.ToListItem).ToList(),
            upcoming,
            viewsByDay);
    }

    private static IQueryable<Article> IncludeList(IQueryable<Article> query)
        => query.Include(a => a.Author).Include(a => a.Category).Include(a => a.FeaturedImage)
            .Include(a => a.ArticleTags).ThenInclude(t => t.Tag)
            .Include(a => a.ArticleTechnologies).ThenInclude(t => t.Technology);

    private static string? Truncate(string? value, int max)
        => value is null ? null : value.Length <= max ? value : value[..max];
}

public sealed class NewsletterService : INewsletterService
{
    private readonly BlogDbContext _db;
    private readonly IEmailService _email;
    private readonly SiteOptions _site;
    private readonly TimeProvider _clock;

    public NewsletterService(BlogDbContext db, IEmailService email, IOptions<SiteOptions> site, TimeProvider clock)
    {
        _db = db;
        _email = email;
        _site = site.Value;
        _clock = clock;
    }

    public async Task SubscribeAsync(string email, CancellationToken cancellationToken)
    {
        email = email.Trim().ToLowerInvariant();
        var existing = await _db.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == email, cancellationToken);
        var token = SecretHasher.GenerateToken();
        if (existing is null)
        {
            existing = NewsletterSubscriber.Create(email, SecretHasher.Hash(token), _clock.GetUtcNow());
            _db.NewsletterSubscribers.Add(existing);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var confirmUrl = $"{_site.Url.TrimEnd('/')}/newsletter/confirm?token={token}";
        await _email.SendAsync(email, $"Confirm your {_site.Name} subscription",
            $"<p>Confirm your subscription by visiting <a href=\"{confirmUrl}\">{confirmUrl}</a>.</p>", cancellationToken);
    }

    public async Task ConfirmAsync(string token, CancellationToken cancellationToken)
    {
        var hash = SecretHasher.Hash(token);
        var subscriber = await _db.NewsletterSubscribers.FirstOrDefaultAsync(s => s.ConfirmTokenHash == hash, cancellationToken)
                         ?? throw AppException.NotFound("Subscription");
        subscriber.Confirm(_clock.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnsubscribeAsync(string email, CancellationToken cancellationToken)
    {
        var subscriber = await _db.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == email.Trim().ToLowerInvariant(), cancellationToken);
        subscriber?.Unsubscribe(_clock.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email queued to {To} with subject {Subject}", to, subject);
        return Task.CompletedTask;
    }
}

public sealed class SettingsService : ISettingsService
{
    private readonly IOptions<SiteOptions> _site;
    private readonly IOptions<PublishingOptions> _publishing;
    private readonly BlogDbContext _db;

    public SettingsService(IOptions<SiteOptions> site, IOptions<PublishingOptions> publishing, BlogDbContext db)
    {
        _site = site;
        _publishing = publishing;
        _db = db;
    }

    public Task<SiteSettingsDto> GetPublicAsync(CancellationToken cancellationToken)
    {
        var s = _site.Value;
        return Task.FromResult(new SiteSettingsDto(
            s.Name, s.Description, s.Url, s.LogoUrl, s.FaviconUrl, s.DefaultAuthorSlug, _publishing.Value.Mode,
            new SocialPublicDto(s.Social.GitHub, s.Social.LinkedIn, s.Social.X, s.Social.YouTube)));
    }

    public async Task<PublishingRulesDto> GetPublishingRulesAsync(CancellationToken cancellationToken)
    {
        var p = _publishing.Value;
        var s = _site.Value;
        var categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).Select(c => c.Slug).ToListAsync(cancellationToken);
        var author = await _db.Authors.AsNoTracking().FirstOrDefaultAsync(a => a.Slug == s.DefaultAuthorSlug, cancellationToken);
        return new PublishingRulesDto(
            p.Mode,
            string.Equals(p.Mode, nameof(PublishingMode.RequireApproval), StringComparison.OrdinalIgnoreCase),
            p.MinimumContentLength,
            p.RequireSeoTitle,
            p.RequireMetaDescription,
            p.RequireFeaturedImage,
            true,
            p.MinimumTagCount,
            p.AllowHtmlContent ? ["Markdown", "Html"] : ["Markdown"],
            categories,
            Enum.GetNames<Difficulty>(),
            Enum.GetNames<ArticleContentType>(),
            author?.DisplayName ?? "Default author",
            new SeoRuleDto(p.SeoTitleMaxLength, p.MetaDescriptionMinLength, p.MetaDescriptionMaxLength),
            new ScheduleRuleDto("UTC", "08:00"));
    }

    public async Task UpdatePublishingModeAsync(string mode, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PublishingMode>(mode, true, out _))
        {
            throw AppException.Validation("INVALID_MODE", "Publishing mode is invalid.");
        }

        var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == "Publishing:Mode", cancellationToken);
        if (setting is null)
        {
            _db.SiteSettings.Add(new SiteSetting("Publishing:Mode", mode, DateTimeOffset.UtcNow));
        }
        else
        {
            setting.SetValue(mode, DateTimeOffset.UtcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
