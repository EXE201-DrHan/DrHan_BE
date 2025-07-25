using DrHan.Application.Commons;
using DrHan.Application.DTOs.Notifications;

namespace DrHan.Application.Interfaces.Services;

public interface IMealNotificationService
{
    /// <summary>
    /// Get user's meal notification settings
    /// </summary>
    Task<AppResponse<UserMealNotificationSettingsDto>> GetUserNotificationSettingsAsync(int userId);
    
    /// <summary>
    /// Update user's meal notification settings
    /// </summary>
    Task<AppResponse<UserMealNotificationSettingsDto>> UpdateNotificationSettingsAsync(int userId, UpdateMealNotificationSettingsDto settings);
    
    /// <summary>
    /// Check for upcoming meals and send notifications
    /// Called by background service
    /// </summary>
    Task ProcessMealNotificationsAsync();
    
    /// <summary>
    /// Send notification for specific meal entry
    /// </summary>
    Task<bool> SendMealNotificationAsync(int userId, int mealEntryId, string notificationType);
    
    /// <summary>
    /// Get notification history for user
    /// </summary>
    Task<AppResponse<List<MealNotificationLogDto>>> GetNotificationHistoryAsync(int userId, int pageNumber = 1, int pageSize = 20);
    
    /// <summary>
    /// Preview what notifications would be sent for user today
    /// </summary>
    Task<AppResponse<List<NotificationPreviewDto>>> PreviewTodaysNotificationsAsync(int userId);
    
    /// <summary>
    /// Test send a sample notification
    /// </summary>
    Task<AppResponse<bool>> SendTestNotificationAsync(int userId);
    
    /// <summary>
    /// Get list of available timezones
    /// </summary>
    Task<AppResponse<List<string>>> GetAvailableTimeZonesAsync();
} 