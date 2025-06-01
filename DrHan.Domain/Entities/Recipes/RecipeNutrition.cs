#nullable disable
namespace DrHan.Domain.Entities.Recipes;

public class RecipeNutrition : BaseEntity
{
    public int? RecipeId { get; set; }
    public string NutrientName { get; set; }

    public decimal? AmountPerServing { get; set; }

    public string Unit { get; set; }

    public decimal? DailyValuePercent { get; set; }
    public virtual Recipe Recipe { get; set; }
}