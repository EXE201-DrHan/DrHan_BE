using DrHan.Domain.Enums;

namespace DrHan.Application.DTOs.Notifications;

public class UserMealNotificationSettingsDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool IsEnabled { get; set; }
    
    // Meal times in HH:mm format
    public string? BreakfastTime { get; set; }
    public string? LunchTime { get; set; }
    public string? DinnerTime { get; set; }
    public string? SnackTime { get; set; }
    
    public int AdvanceNoticeMinutes { get; set; }
    
    // Quiet hours in HH:mm format
    public string? QuietStartTime { get; set; }
    public string? QuietEndTime { get; set; }
    
    public DaysOfWeek EnabledDays { get; set; }
    public string TimeZone { get; set; } = "SE Asia Standard Time"; // Always Vietnam timezone
    
    // Individual meal controls
    public bool BreakfastEnabled { get; set; }
    public bool LunchEnabled { get; set; }
    public bool DinnerEnabled { get; set; }
    public bool SnackEnabled { get; set; }
}

public class UpdateMealNotificationSettingsDto
{
    public bool IsEnabled { get; set; }
    
    // Times in HH:mm format (e.g., "08:00", "12:30")
    public string? BreakfastTime { get; set; }
    public string? LunchTime { get; set; }
    public string? DinnerTime { get; set; }
    public string? SnackTime { get; set; }
    
    public int AdvanceNoticeMinutes { get; set; } = 30;
    
    public string? QuietStartTime { get; set; }
    public string? QuietEndTime { get; set; }
    
    public DaysOfWeek EnabledDays { get; set; } = DaysOfWeek.All;
    public string TimeZone { get; set; } = "SE Asia Standard Time"; // Always Vietnam timezone
    
    public bool BreakfastEnabled { get; set; } = true;
    public bool LunchEnabled { get; set; } = true;
    public bool DinnerEnabled { get; set; } = true;
    public bool SnackEnabled { get; set; } = true;
}

public class MealNotificationLogDto
{
    public int Id { get; set; }
    public DateTime ScheduledTime { get; set; }
    public DateTime? SentAt { get; set; }
    public string NotificationType { get; set; }
    public string MealType { get; set; }
    public DateOnly MealDate { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? NotificationContent { get; set; }
    public string? MealName { get; set; }
}

// For notification preview/testing
public class NotificationPreviewDto
{
    public string Title { get; set; }
    public string Message { get; set; }
    public string MealType { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string TimeUntilMeal { get; set; }
} 