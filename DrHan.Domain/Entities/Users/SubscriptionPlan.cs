#nullable disable
namespace DrHan.Domain.Entities.Users;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; }

    public string Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; }

    public string BillingCycle { get; set; }

    public int? UsageQuota { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();

    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}