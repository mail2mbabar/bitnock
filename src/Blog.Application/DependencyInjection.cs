using Blog.Application.Ai;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Validation.UpsertArticleRequestValidator>();
        services.AddSingleton<IAiContentService, NoOpAiContentService>();
        services.AddSingleton<IAiSeoService, NoOpAiSeoService>();
        services.AddSingleton<IAiImageService, NoOpAiImageService>();
        services.AddSingleton<IAiContentReviewService, NoOpAiContentReviewService>();
        return services;
    }
}
