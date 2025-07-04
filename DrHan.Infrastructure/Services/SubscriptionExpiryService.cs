//using DrHan.Application.Interfaces.Repository;
//using DrHan.Domain.Entities.Users;
//using DrHan.Domain.Constants.Status;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.EntityFrameworkCore;

//namespace DrHan.Infrastructure.Services;

//public class SubscriptionExpiryService : BackgroundService
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<SubscriptionExpiryService> _logger;
//    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

//    public SubscriptionExpiryService(
//        IServiceProvider serviceProvider,
//        ILogger<SubscriptionExpiryService> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _logger = logger;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                await CheckExpiredSubscriptions();
//                await Task.Delay(_checkInterval, stoppingToken);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error occurred while checking expired subscriptions");
//                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes on error
//            }
//        }
//    }

//    private async Task CheckExpiredSubscriptions()
//    {
//        using var scope = _serviceProvider.CreateScope();
//        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

//        var expiredSubscriptions = await unitOfWork.Repository<UserSubscription>()
//            .ListAsync(filter: s => s.Status == UserSubscriptionStatus.Active &&
//                       s.EndDate.HasValue &&
//                       s.EndDate <= DateTime.UtcNow);

//        if (expiredSubscriptions.Any())
//        {
//            _logger.LogInformation("Found {Count} expired subscriptions to process", expiredSubscriptions.Count);

//            foreach (var subscription in expiredSubscriptions)
//            {
//                subscription.Status = UserSubscriptionStatus.Expired;
//                unitOfWork.Repository<UserSubscription>().Update(subscription);
                
//                _logger.LogInformation("Subscription {SubscriptionId} for user {UserId} marked as expired", 
//                    subscription.Id, subscription.UserId);
//            }

//            await unitOfWork.CompleteAsync();
//            _logger.LogInformation("Processed {Count} expired subscriptions", expiredSubscriptions.Count);
//        }

//        // Check for subscriptions expiring soon (within 7 days) for renewal notifications
//        await CheckSubscriptionsExpiringSoon(unitOfWork);
//    }

//    private async Task CheckSubscriptionsExpiringSoon(IUnitOfWork unitOfWork)
//    {
//        var soonToExpireDate = DateTime.UtcNow.AddDays(7);
        
//        var subscriptionsExpiringSoon = await unitOfWork.Repository<UserSubscription>()
//            .ListAsync(filter: s => s.Status == UserSubscriptionStatus.Active &&
//                           s.EndDate.HasValue &&
//                           s.EndDate <= soonToExpireDate &&
//                           s.EndDate > DateTime.UtcNow);

//        if (subscriptionsExpiringSoon.Any())
//        {
//            _logger.LogInformation("Found {Count} subscriptions expiring within 7 days", subscriptionsExpiringSoon.Count);

//            foreach (var subscription in subscriptionsExpiringSoon)
//            {
//                // Here you could send renewal notifications
//                // For now, just log
//                _logger.LogInformation("Subscription {SubscriptionId} for user {UserId} expires on {ExpiryDate}", 
//                    subscription.Id, subscription.UserId, subscription.EndDate);
                
//                // TODO: Add notification service integration
//                // await _notificationService.SendRenewalNotification(subscription);
//            }
//        }
//    }
//} 