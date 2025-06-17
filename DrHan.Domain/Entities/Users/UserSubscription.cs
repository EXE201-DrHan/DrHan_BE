#nullable disable
namespace DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;

public class UserSubscription : BaseEntity
{
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public UserSubscriptionStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public virtual SubscriptionPlan Plan { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; }
}