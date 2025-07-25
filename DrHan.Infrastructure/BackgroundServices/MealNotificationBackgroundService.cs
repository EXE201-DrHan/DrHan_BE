using DrHan.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.BackgroundServices;

public class MealNotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MealNotificationBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public MealNotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MealNotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Meal Notification Background Service started");

        // Wait a bit on startup to let the application fully initialize
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMealNotifications();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing meal notifications");
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

        _logger.LogInformation("Meal Notification Background Service stopped");
    }

    private async Task ProcessMealNotifications()
    {
        // Use await using for async disposal
        await using var scope = _serviceProvider.CreateAsyncScope();
        var mealNotificationService = scope.ServiceProvider.GetRequiredService<IMealNotificationService>();

        try
        {
            _logger.LogDebug("Starting meal notification processing cycle");
            await mealNotificationService.ProcessMealNotificationsAsync();
            _logger.LogDebug("Completed meal notification processing cycle");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in meal notification processing cycle");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Meal Notification Background Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}