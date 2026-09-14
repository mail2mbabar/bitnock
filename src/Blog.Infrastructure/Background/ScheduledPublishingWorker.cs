using Blog.Application.Articles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blog.Infrastructure.Background;

public sealed class ScheduledPublishingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledPublishingService> _logger;

    public ScheduledPublishingService(IServiceScopeFactory scopeFactory, ILogger<ScheduledPublishingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled publishing worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var articles = scope.ServiceProvider.GetRequiredService<IArticleService>();
                var published = await articles.PublishDueScheduledAsync(stoppingToken);
                if (published > 0)
                {
                    _logger.LogInformation("Published {Count} scheduled articles", published);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scheduled publishing cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
