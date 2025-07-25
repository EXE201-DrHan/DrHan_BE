using DrHan.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.BackgroundServices;

public class RecipePersistenceBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecipePersistenceBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1); // Process queue every minute

    public RecipePersistenceBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RecipePersistenceBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recipe Persistence Background Service started");

        // Wait a bit on startup to let the application fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRecipePersistenceQueue(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing recipe persistence queue");
            }

            try
            {
                await Task.Delay(_period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                break;
            }
        }

        _logger.LogInformation("Recipe Persistence Background Service stopped");
    }

    private async Task ProcessRecipePersistenceQueue(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var recipePersistenceService = scope.ServiceProvider.GetRequiredService<IRecipePersistenceService>();

        try
        {
            _logger.LogDebug("Processing recipe persistence queue");
            await recipePersistenceService.ProcessQueuedRecipesAsync(cancellationToken);
            _logger.LogDebug("Completed recipe persistence queue processing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in recipe persistence queue processing");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recipe Persistence Background Service is stopping");
        
        // Process any remaining items in the queue before stopping
        try
        {
            await ProcessRecipePersistenceQueue(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing final queue items during shutdown");
        }
        
        await base.StopAsync(stoppingToken);
    }
}