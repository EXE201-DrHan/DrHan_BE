#nullable disable
using DrHan.Domain.Entities.Families;
using DrHan.Domain.Entities.Users;

namespace DrHan.Domain.Entities.MealPlans;

public class MealPlan : BaseEntity
{
    public int? UserId { get; set; }
    public int? FamilyId { get; set; }
    public string Name { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string PlanType { get; set; }

    public string Notes { get; set; }
    public virtual Family Family { get; set; }

    public virtual ICollection<MealPlanEntry> MealPlanEntries { get; set; } = new List<MealPlanEntry>();

    public virtual ICollection<MealPlanShoppingItem> MealPlanShoppingItems { get; set; } =
        new List<MealPlanShoppingItem>();

    public virtual ApplicationUser ApplicationUser { get; set; }
}