using Blog.Application.Articles;
using Blog.Domain.Enums;
using FluentValidation;

namespace Blog.Application.Validation;

public sealed class UpsertArticleRequestValidator : AbstractValidator<UpsertArticleRequest>
{
    public UpsertArticleRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Excerpt).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Slug).MaximumLength(180).When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.MetaTitle).MaximumLength(70).When(x => !string.IsNullOrWhiteSpace(x.MetaTitle));
        RuleFor(x => x.MetaDescription).MaximumLength(180).When(x => !string.IsNullOrWhiteSpace(x.MetaDescription));
        RuleFor(x => x.CanonicalUrl).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.CanonicalUrl));
        RuleFor(x => x.Difficulty)
            .Must(v => v is null || Enum.TryParse<Difficulty>(v, true, out _))
            .WithMessage("Difficulty is invalid.");
        RuleFor(x => x.ContentType)
            .Must(v => v is null || Enum.TryParse<ArticleContentType>(v, true, out _))
            .WithMessage("Content type is invalid.");
    }
}

public sealed class ScheduleArticleRequestValidator : AbstractValidator<ScheduleArticleRequest>
{
    public ScheduleArticleRequestValidator()
    {
        RuleFor(x => x.ScheduledAt).Must(v => v > DateTimeOffset.UtcNow.AddMinutes(-1))
            .WithMessage("Scheduled time must be in the future.");
    }
}
