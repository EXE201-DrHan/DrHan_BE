#nullable disable
namespace DrHan.Domain.Entities.Users;

public class PlanFeature : BaseEntity
{
    public long Id { get; set; }
    public int PlanId { get; set; }
    public string FeatureName { get; set; }

    public string Description { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
    public virtual SubscriptionPlan Plan { get; set; }
}