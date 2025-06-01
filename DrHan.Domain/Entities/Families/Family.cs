#nullable disable
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Users;

namespace DrHan.Domain.Entities.Families;

public class Family : BaseEntity
{
    public string Name { get; set; }
    public int? CreatedBy { get; set; }
    public string FamilyCode { get; set; }

    public int? MaxMembers { get; set; }
    public virtual ApplicationUser CreatedByNavigation { get; set; }

    public virtual ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();

    public virtual ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
}