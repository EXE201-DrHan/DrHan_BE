#nullable disable
using DrHan.Domain.Entities.FoodProducts;
using DrHan.Domain.Entities.Recipes;

namespace DrHan.Domain.Entities.MealPlans;

public class MealPlanEntry : BaseEntity
{
    public int? MealPlanId { get; set; }
    public DateOnly MealDate { get; set; }

    public string MealType { get; set; }
    public int? RecipeId { get; set; }
    public int? ProductId { get; set; }
    public string CustomMealName { get; set; }

    public decimal? Servings { get; set; }

    public string Notes { get; set; }
    public virtual MealPlan MealPlan { get; set; }

    public virtual FoodProduct Product { get; set; }

    public virtual Recipe Recipe { get; set; }
}