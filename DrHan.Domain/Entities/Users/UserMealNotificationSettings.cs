#nullable disable
using DrHan.Domain.Enums;

namespace DrHan.Domain.Entities.Users;

public class UserMealNotificationSettings : BaseEntity
{
    public int UserId { get; set; }
    public bool IsEnabled { get; set; } = true;
    
    // Custom meal times (user's preferred eating schedule)
    public TimeOnly? BreakfastTime { get; set; } = new TimeOnly(8, 0);   // 8:00 AM
    public TimeOnly? LunchTime { get; set; } = new TimeOnly(12, 0);      // 12:00 PM  
    public TimeOnly? DinnerTime { get; set; } = new TimeOnly(18, 30);    // 6:30 PM
    public TimeOnly? SnackTime { get; set; } = new TimeOnly(15, 0);      // 3:00 PM
    
    // How many minutes before meal time to send notification
    public int AdvanceNoticeMinutes { get; set; } = 30;
    
    // Quiet hours - no notifications during this time
    public TimeOnly? QuietStartTime { get; set; } = new TimeOnly(22, 0); // 10:00 PM
    public TimeOnly? QuietEndTime { get; set; } = new TimeOnly(7, 0);    // 7:00 AM
    
    // Which days to send notifications
    public DaysOfWeek EnabledDays { get; set; } = DaysOfWeek.All;
    
    // User's timezone for accurate scheduling (always Vietnam timezone)
    public string TimeZone { get; set; } = "SE Asia Standard Time";
    
    // Individual meal type controls
    public bool BreakfastEnabled { get; set; } = true;
    public bool LunchEnabled { get; set; } = true;
    public bool DinnerEnabled { get; set; } = true;
    public bool SnackEnabled { get; set; } = true;
    
    // Navigation property
    public virtual ApplicationUser User { get; set; }
} 