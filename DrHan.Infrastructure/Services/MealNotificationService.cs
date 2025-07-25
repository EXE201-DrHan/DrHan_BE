using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Notifications;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Constants;
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Notifications;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Services;

public class MealNotificationService : IMealNotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<MealNotificationService> _logger;
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            // Try Windows time zone ID first
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Try IANA time zone ID (Linux/macOS)
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: create a custom time zone for UTC+7
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Vietnam Standard Time",
                    TimeSpan.FromHours(7),
                    "Vietnam Standard Time",
                    "Vietnam Standard Time"
                );
            }
        }
    }

    public MealNotificationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPushNotificationService pushNotificationService,
        ILogger<MealNotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    public async Task<AppResponse<UserMealNotificationSettingsDto>> GetUserNotificationSettingsAsync(int userId)
    {
        var response = new AppResponse<UserMealNotificationSettingsDto>();

        try
        {
            var settings = await _unitOfWork.Repository<UserMealNotificationSettings>()
                .FindAsync(s => s.UserId == userId);

            if (settings == null)
            {
                // Create default settings for user (always Vietnam timezone)
                settings = new UserMealNotificationSettings
                {
                    UserId = userId,
                    IsEnabled = true,
                    BreakfastTime = new TimeOnly(8, 0),
                    LunchTime = new TimeOnly(12, 0),
                    DinnerTime = new TimeOnly(18, 30),
                    SnackTime = new TimeOnly(15, 0),
                    AdvanceNoticeMinutes = 30,
                    QuietStartTime = new TimeOnly(22, 0),
                    QuietEndTime = new TimeOnly(7, 0),
                    EnabledDays = DaysOfWeek.All,
                    TimeZone = VietnamTimeZone.Id  // Always Vietnam timezone
                };

                await _unitOfWork.Repository<UserMealNotificationSettings>().AddAsync(settings);
                await _unitOfWork.CompleteAsync();
            }

            var settingsDto = _mapper.Map<UserMealNotificationSettingsDto>(settings);
            return response.SetSuccessResponse(settingsDto, "Success", "Notification settings retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to retrieve notification settings");
        }
    }

    public async Task<AppResponse<UserMealNotificationSettingsDto>> UpdateNotificationSettingsAsync(int userId, UpdateMealNotificationSettingsDto updateDto)
    {
        var response = new AppResponse<UserMealNotificationSettingsDto>();

        try
        {
            var settings = await _unitOfWork.Repository<UserMealNotificationSettings>()
                .FindAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new UserMealNotificationSettings { UserId = userId };
                await _unitOfWork.Repository<UserMealNotificationSettings>().AddAsync(settings);
            }

            // Update settings
            settings.IsEnabled = updateDto.IsEnabled;
            settings.AdvanceNoticeMinutes = updateDto.AdvanceNoticeMinutes;
            settings.EnabledDays = updateDto.EnabledDays;
            
            // Always use Vietnam timezone for notifications
            settings.TimeZone = VietnamTimeZone.Id;
            
            settings.BreakfastEnabled = updateDto.BreakfastEnabled;
            settings.LunchEnabled = updateDto.LunchEnabled;
            settings.DinnerEnabled = updateDto.DinnerEnabled;
            settings.SnackEnabled = updateDto.SnackEnabled;

            // Parse and update times
            if (TimeOnly.TryParse(updateDto.BreakfastTime, out var breakfastTime))
                settings.BreakfastTime = breakfastTime;
            if (TimeOnly.TryParse(updateDto.LunchTime, out var lunchTime))
                settings.LunchTime = lunchTime;
            if (TimeOnly.TryParse(updateDto.DinnerTime, out var dinnerTime))
                settings.DinnerTime = dinnerTime;
            if (TimeOnly.TryParse(updateDto.SnackTime, out var snackTime))
                settings.SnackTime = snackTime;
            if (TimeOnly.TryParse(updateDto.QuietStartTime, out var quietStart))
                settings.QuietStartTime = quietStart;
            if (TimeOnly.TryParse(updateDto.QuietEndTime, out var quietEnd))
                settings.QuietEndTime = quietEnd;

            _unitOfWork.Repository<UserMealNotificationSettings>().Update(settings);
            await _unitOfWork.CompleteAsync();

            var settingsDto = _mapper.Map<UserMealNotificationSettingsDto>(settings);
            return response.SetSuccessResponse(settingsDto, "Success", "Notification settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to update notification settings");
        }
    }

    public async Task ProcessMealNotificationsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            var tomorrow = today.AddDays(1);

            _logger.LogInformation("Processing meal notifications at {Now}", now);

            // Get all users with enabled notifications
            var usersWithNotifications = await _unitOfWork.Repository<UserMealNotificationSettings>()
                .ListAsync(
                    filter: s => s.IsEnabled,
                    includeProperties: query => query.Include(s => s.User)
                );

            var processedCount = 0;

            foreach (var settings in usersWithNotifications)
            {
                await ProcessUserMealNotificationsAsync(settings, now, today);
                processedCount++;
            }

            _logger.LogInformation("Processed meal notifications for {Count} users", processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing meal notifications");
        }
    }

    private async Task ProcessUserMealNotificationsAsync(UserMealNotificationSettings settings, DateTime now, DateOnly today)
    {
        try
        {
            // Always use Vietnam time for notification processing
            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(now, VietnamTimeZone);
            var vietnamDate = DateOnly.FromDateTime(vietnamTime);

            _logger.LogDebug("Processing notifications for user {UserId} at Vietnam time {VietnamTime}", 
                settings.UserId, vietnamTime);

            // Check if notifications are enabled for current day
            var currentDayOfWeek = GetDayOfWeekFlag(vietnamTime.DayOfWeek);
            if (!settings.EnabledDays.HasFlag(currentDayOfWeek))
            {
                _logger.LogDebug("Notifications disabled for day {DayOfWeek} for user {UserId}", 
                    vietnamTime.DayOfWeek, settings.UserId);
                return;
            }

            // Check quiet hours
            if (IsInQuietHours(TimeOnly.FromDateTime(vietnamTime), settings))
            {
                _logger.LogDebug("Currently in quiet hours for user {UserId}", settings.UserId);
                return;
            }

            // Get user's meal plan entries for today (Vietnam date)
            var mealEntries = await GetUserMealEntriesForDateAsync(settings.UserId, vietnamDate);

            // Process each meal type using Vietnam time
            await ProcessMealTypeNotifications(settings, mealEntries, MealTypeConstants.BREAKFAST, settings.BreakfastTime, settings.BreakfastEnabled, vietnamTime);
            await ProcessMealTypeNotifications(settings, mealEntries, MealTypeConstants.LUNCH, settings.LunchTime, settings.LunchEnabled, vietnamTime);
            await ProcessMealTypeNotifications(settings, mealEntries, MealTypeConstants.DINNER, settings.DinnerTime, settings.DinnerEnabled, vietnamTime);
            await ProcessMealTypeNotifications(settings, mealEntries, MealTypeConstants.SNACK, settings.SnackTime, settings.SnackEnabled, vietnamTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notifications for user {UserId}", settings.UserId);
        }
    }

    private async Task ProcessMealTypeNotifications(
        UserMealNotificationSettings settings,
        List<MealPlanEntry> mealEntries,
        string mealType,
        TimeOnly? mealTime,
        bool isEnabled,
        DateTime userLocalTime)
    {
        if (!isEnabled || !mealTime.HasValue)
            return;

        // Find meal entry for this meal type
        var mealEntry = mealEntries.FirstOrDefault(me => me.MealType == mealType);
        if (mealEntry == null)
            return; // No meal planned - this is the key difference from Option A

        var mealDateTime = userLocalTime.Date.Add(mealTime.Value.ToTimeSpan());
        var timeDifference = (mealDateTime - userLocalTime).TotalMinutes;

        // Check for upcoming meal notification (advance notice)
        if (Math.Abs(timeDifference - settings.AdvanceNoticeMinutes) <= 2) // Within 2 minutes tolerance
        {
            await SendMealNotificationIfNotSent(settings.UserId, mealEntry, NotificationTypes.UPCOMING_MEAL, mealDateTime.AddMinutes(-settings.AdvanceNoticeMinutes));
        }

        // Check for meal time notification
        if (Math.Abs(timeDifference) <= 2) // Within 2 minutes of meal time
        {
            await SendMealNotificationIfNotSent(settings.UserId, mealEntry, NotificationTypes.MEAL_TIME, mealDateTime);
        }

        // Check for missed meal notification (2 hours after meal time)
        if (timeDifference <= -120 && timeDifference >= -125) // 2 hours after, within 5 minutes tolerance
        {
            if (!mealEntry.IsCompleted)
            {
                await SendMealNotificationIfNotSent(settings.UserId, mealEntry, NotificationTypes.MISSED_MEAL, mealDateTime.AddHours(2));
            }
        }
    }

    private async Task<List<MealPlanEntry>> GetUserMealEntriesForDateAsync(int userId, DateOnly date)
    {
        // Get all meal entries for the date and include meal plan, then filter by user
        var allMealEntries = await _unitOfWork.Repository<MealPlanEntry>()
            .ListAsync(
                filter: me => me.MealDate == date,
                includeProperties: query => query
                    .Include(me => me.Recipe)
                    .Include(me => me.Product)
                    .Include(me => me.MealPlan)
            );

        // Filter by user ID after loading (since we now have the MealPlan loaded)
        return allMealEntries.Where(me => me.MealPlan != null && me.MealPlan.UserId == userId).ToList();
    }

    private async Task SendMealNotificationIfNotSent(int userId, MealPlanEntry mealEntry, string notificationType, DateTime scheduledTime)
    {
        // Check if notification already sent
        var existingLog = await _unitOfWork.Repository<MealNotificationLog>()
            .FindAsync(log => log.UserId == userId &&
                             log.MealPlanEntryId == mealEntry.Id &&
                             log.NotificationType == notificationType &&
                             log.MealDate == mealEntry.MealDate);

        if (existingLog != null && existingLog.IsSuccessful)
        {
            return; // Already sent successfully
        }

        var success = await SendMealNotificationAsync(userId, mealEntry.Id, notificationType);

        // Log the notification attempt
        var log = new MealNotificationLog
        {
            UserId = userId,
            MealPlanEntryId = mealEntry.Id,
            MealPlanId = mealEntry.MealPlanId,
            ScheduledTime = scheduledTime,
            SentAt = DateTime.UtcNow,
            NotificationType = notificationType,
            MealType = mealEntry.MealType,
            MealDate = mealEntry.MealDate,
            IsSuccessful = success,
            NotificationContent = GenerateNotificationContent(mealEntry, notificationType)
        };

        await _unitOfWork.Repository<MealNotificationLog>().AddAsync(log);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<bool> SendMealNotificationAsync(int userId, int mealEntryId, string notificationType)
    {
        try
        {
            var mealEntry = await _unitOfWork.Repository<MealPlanEntry>()
                .ListAsync(
                    filter: me => me.Id == mealEntryId,
                    includeProperties: query => query.Include(me => me.Recipe).Include(me => me.Product)
                );

            var entry = mealEntry.FirstOrDefault();
            if (entry == null)
                return false;

            var (title, message) = GenerateNotificationTitleAndMessage(entry, notificationType);
            var actionUrl = $"/meal-plans/{entry.MealPlanId}";

            return await _pushNotificationService.SendGeneralNotificationAsync(userId, title, message, actionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending meal notification for user {UserId}, meal entry {MealEntryId}", userId, mealEntryId);
            return false;
        }
    }

    private (string title, string message) GenerateNotificationTitleAndMessage(MealPlanEntry mealEntry, string notificationType)
    {
        var mealName = GetMealDisplayName(mealEntry);
        var mealType = mealEntry.MealType.ToLower();

        return notificationType switch
        {
            NotificationTypes.UPCOMING_MEAL => ($"Upcoming {mealEntry.MealType}", $"Your {mealType} is coming up soon: {mealName} 🍽️"),
            NotificationTypes.MEAL_TIME => ($"Time for {mealEntry.MealType}!", $"It's time to enjoy your {mealType}: {mealName} 😋"),
            NotificationTypes.MISSED_MEAL => ($"Did you have {mealType}?", $"Don't forget to mark your {mealType} as complete: {mealName} ✅"),
            _ => ("Meal Reminder", $"You have {mealName} planned for {mealType}")
        };
    }

    private string GetMealDisplayName(MealPlanEntry mealEntry)
    {
        return mealEntry.Recipe?.Name ?? mealEntry.Product?.Name ?? mealEntry.CustomMealName ?? "your meal";
    }

    private string GenerateNotificationContent(MealPlanEntry mealEntry, string notificationType)
    {
        var mealName = GetMealDisplayName(mealEntry);
        return $"{notificationType}: {mealEntry.MealType} - {mealName}";
    }

    private bool IsInQuietHours(TimeOnly currentTime, UserMealNotificationSettings settings)
    {
        if (!settings.QuietStartTime.HasValue || !settings.QuietEndTime.HasValue)
            return false;

        var quietStart = settings.QuietStartTime.Value;
        var quietEnd = settings.QuietEndTime.Value;

        // Handle overnight quiet hours (e.g., 22:00 to 07:00)
        if (quietStart > quietEnd)
        {
            return currentTime >= quietStart || currentTime <= quietEnd;
        }
        else
        {
            return currentTime >= quietStart && currentTime <= quietEnd;
        }
    }

    private DaysOfWeek GetDayOfWeekFlag(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => DaysOfWeek.Monday,
            DayOfWeek.Tuesday => DaysOfWeek.Tuesday,
            DayOfWeek.Wednesday => DaysOfWeek.Wednesday,
            DayOfWeek.Thursday => DaysOfWeek.Thursday,
            DayOfWeek.Friday => DaysOfWeek.Friday,
            DayOfWeek.Saturday => DaysOfWeek.Saturday,
            DayOfWeek.Sunday => DaysOfWeek.Sunday,
            _ => DaysOfWeek.None
        };
    }

    public async Task<AppResponse<List<MealNotificationLogDto>>> GetNotificationHistoryAsync(int userId, int pageNumber = 1, int pageSize = 20)
    {
        var response = new AppResponse<List<MealNotificationLogDto>>();

        try
        {
            var logs = await _unitOfWork.Repository<MealNotificationLog>()
                .ListAsync(
                    filter: log => log.UserId == userId,
                    orderBy: query => query.OrderByDescending(log => log.SentAt ?? log.ScheduledTime)
                );

            // Apply pagination manually
            var paginatedLogs = logs
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var logDtos = _mapper.Map<List<MealNotificationLogDto>>(paginatedLogs);
            return response.SetSuccessResponse(logDtos, "Success", "Notification history retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to retrieve notification history");
        }
    }

    public async Task<AppResponse<List<NotificationPreviewDto>>> PreviewTodaysNotificationsAsync(int userId)
    {
        var response = new AppResponse<List<NotificationPreviewDto>>();

        try
        {
            var settings = await _unitOfWork.Repository<UserMealNotificationSettings>()
                .FindAsync(s => s.UserId == userId);

            if (settings == null || !settings.IsEnabled)
            {
                return response.SetSuccessResponse(new List<NotificationPreviewDto>(), "Success", "No notifications configured");
            }

            // Always use Vietnam time for preview
            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
            var today = DateOnly.FromDateTime(vietnamTime);

            var mealEntries = await GetUserMealEntriesForDateAsync(userId, today);
            var previews = new List<NotificationPreviewDto>();

            // Generate previews for each meal type that has entries
            foreach (var entry in mealEntries)
            {
                var mealTime = GetMealTimeForType(entry.MealType, settings);
                if (mealTime.HasValue)
                {
                    var mealDateTime = today.ToDateTime(mealTime.Value);
                    var (title, message) = GenerateNotificationTitleAndMessage(entry, NotificationTypes.UPCOMING_MEAL);

                    previews.Add(new NotificationPreviewDto
                    {
                        Title = title,
                        Message = message,
                        MealType = entry.MealType,
                        ScheduledTime = mealDateTime.AddMinutes(-settings.AdvanceNoticeMinutes),
                        TimeUntilMeal = FormatTimeUntilMeal(mealDateTime, vietnamTime)
                    });
                }
            }

            return response.SetSuccessResponse(previews.OrderBy(p => p.ScheduledTime).ToList(), "Success", "Notification preview generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating notification preview for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to generate notification preview");
        }
    }

    public async Task<AppResponse<bool>> SendTestNotificationAsync(int userId)
    {
        var response = new AppResponse<bool>();

        try
        {
            var title = "🍽️ DrHan Meal Reminder Test";
            var message = "This is a test notification. Your meal notifications are working perfectly!";
            var actionUrl = "/meal-plans";

            var success = await _pushNotificationService.SendGeneralNotificationAsync(userId, title, message, actionUrl);
            
            if (success)
            {
                return response.SetSuccessResponse(true, "Success", "Test notification sent successfully");
            }
            else
            {
                return response.SetErrorResponse("Error", "Failed to send test notification");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification for user {UserId}", userId);
            return response.SetErrorResponse("Error", "Failed to send test notification");
        }
    }

    private TimeOnly? GetMealTimeForType(string mealType, UserMealNotificationSettings settings)
    {
        return mealType switch
        {
            MealTypeConstants.BREAKFAST => settings.BreakfastTime,
            MealTypeConstants.LUNCH => settings.LunchTime,
            MealTypeConstants.DINNER => settings.DinnerTime,
            MealTypeConstants.SNACK => settings.SnackTime,
            _ => null
        };
    }

    private string FormatTimeUntilMeal(DateTime mealTime, DateTime currentTime)
    {
        var timeSpan = mealTime - currentTime;
        
        if (timeSpan.TotalMinutes < 0)
            return "Past";
        
        if (timeSpan.TotalHours < 1)
            return $"{(int)timeSpan.TotalMinutes} minutes";
        
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours {timeSpan.Minutes} minutes";
        
        return $"{(int)timeSpan.TotalDays} days";
    }

    public async Task<AppResponse<List<string>>> GetAvailableTimeZonesAsync()
    {
        var response = new AppResponse<List<string>>();

        try
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones()
                .Select(tz => tz.Id)
                .OrderBy(id => id)
                .ToList();

            return response.SetSuccessResponse(timeZones, "Success", $"Found {timeZones.Count} available timezones");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available timezones");
            return response.SetErrorResponse("Error", "Failed to retrieve available timezones");
        }
    }
} 