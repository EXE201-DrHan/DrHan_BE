using DrHan.Domain.Constants.Status;

namespace DrHan.Application.DTOs.Subscription;

public class SubscriptionResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal PlanPrice { get; set; }
    public string Currency { get; set; } = "VND";
    public string BillingCycle { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public UserSubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int? DaysRemaining { get; set; }
}

public class CreateSubscriptionRequestDto
{
    public int PlanId { get; set; }
}

public class UpgradeSubscriptionRequestDto
{
    public int NewPlanId { get; set; }
}

public class SubscriptionStatusDto
{
    public bool HasActiveSubscription { get; set; }
    public SubscriptionResponseDto? CurrentSubscription { get; set; }
    public Dictionary<string, object>? PlanLimits { get; set; }
    public Dictionary<string, int>? CurrentUsage { get; set; }
} 