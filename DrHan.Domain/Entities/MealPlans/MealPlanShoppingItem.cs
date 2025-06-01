#nullable disable
using DrHan.Domain.Entities.Ingredients;

namespace DrHan.Domain.Entities.MealPlans;

public class MealPlanShoppingItem : BaseEntity
{
    public int? MealPlanId { get; set; }
    public int? IngredientId { get; set; }
    public string IngredientName { get; set; }

    public decimal? Quantity { get; set; }

    public string Unit { get; set; }

    public string Category { get; set; }

    public bool? IsPurchased { get; set; }

    public decimal? EstimatedCost { get; set; }
    public virtual Ingredient Ingredient { get; set; }

    public virtual MealPlan MealPlan { get; set; }
}