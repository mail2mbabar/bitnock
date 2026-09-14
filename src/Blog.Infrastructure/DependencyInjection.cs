using Blog.Application.Agents;
using Blog.Application.Articles;
using Blog.Application.Auth;
using Blog.Application.Media;
using Blog.Application.Taxonomy;
using Blog.Infrastructure.Background;
using Blog.Infrastructure.Identity;
using Blog.Infrastructure.Persistence;
using Blog.Infrastructure.Seeding;
using Blog.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
                               ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<BlogDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(BlogDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(5);
            });
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 8;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<BlogDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IArticleValidationService, ArticleValidationService>();
        services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<ITaxonomyService, TaxonomyService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IStorageService, LocalStorageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAgentCredentialService, AgentCredentialService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISearchService, PostgresSearchService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<INewsletterService, NewsletterService>();
        services.AddScoped<IEmailService, LoggingEmailService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ISeoDocumentService, SeoDocumentService>();
        services.AddSingleton<IMarkdownService, MarkdownService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddHostedService<ScheduledPublishingService>();

        return services;
    }
}
