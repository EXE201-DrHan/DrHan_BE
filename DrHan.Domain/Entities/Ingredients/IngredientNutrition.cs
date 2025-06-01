#nullable disable
namespace DrHan.Domain.Entities.Ingredients;

public class IngredientNutrition : BaseEntity
{
    public int? IngredientId { get; set; }
    public string NutrientName { get; set; }

    public decimal? AmountPer100g { get; set; }

    public string Unit { get; set; }
    public virtual Ingredient Ingredient { get; set; }
}