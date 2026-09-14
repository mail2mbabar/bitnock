namespace Blog.Application.Ai;

/// <summary>
/// Extension point for a future AI drafting service. The platform does not call a specific vendor.
/// </summary>
public interface IAiContentService
{
    Task<string?> SuggestOutlineAsync(string topic, CancellationToken cancellationToken);
}

public interface IAiSeoService
{
    Task<AiSeoSuggestion?> SuggestAsync(string title, string content, CancellationToken cancellationToken);
}

public interface IAiImageService
{
    Task<Stream?> GenerateSocialImageAsync(string title, string category, CancellationToken cancellationToken);
}

public interface IAiContentReviewService
{
    Task<IReadOnlyList<string>> ReviewAsync(string content, CancellationToken cancellationToken);
}

public sealed record AiSeoSuggestion(string MetaTitle, string MetaDescription, IReadOnlyList<string> Tags);

public sealed class NoOpAiContentService : IAiContentService
{
    public Task<string?> SuggestOutlineAsync(string topic, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);
}

public sealed class NoOpAiSeoService : IAiSeoService
{
    public Task<AiSeoSuggestion?> SuggestAsync(string title, string content, CancellationToken cancellationToken)
        => Task.FromResult<AiSeoSuggestion?>(null);
}

public sealed class NoOpAiImageService : IAiImageService
{
    public Task<Stream?> GenerateSocialImageAsync(string title, string category, CancellationToken cancellationToken)
        => Task.FromResult<Stream?>(null);
}

public sealed class NoOpAiContentReviewService : IAiContentReviewService
{
    public Task<IReadOnlyList<string>> ReviewAsync(string content, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([]);
}
