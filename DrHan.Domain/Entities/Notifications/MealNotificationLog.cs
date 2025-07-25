#nullable disable

namespace DrHan.Domain.Entities.Notifications;

public class MealNotificationLog : BaseEntity
{
    public int UserId { get; set; }
    public int? MealPlanEntryId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public DateTime? SentAt { get; set; }
    public string NotificationType { get; set; } // "UPCOMING_MEAL", "MEAL_TIME", "MISSED_MEAL"
    public string MealType { get; set; } // "Breakfast", "Lunch", "Dinner", "Snack"
    public DateOnly MealDate { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? NotificationContent { get; set; }
    
    // For tracking which meal plan this relates to
    public int? MealPlanId { get; set; }
}

public static class NotificationTypes
{
    public const string UPCOMING_MEAL = "UPCOMING_MEAL";   // 30 minutes before
    public const string MEAL_TIME = "MEAL_TIME";           // At meal time
    public const string MISSED_MEAL = "MISSED_MEAL";       // 2 hours after meal time
    public const string PLAN_REMINDER = "PLAN_REMINDER";   // When no meals planned
} 