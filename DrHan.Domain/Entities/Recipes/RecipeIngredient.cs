#nullable disable
using DrHan.Domain.Entities.Ingredients;

namespace DrHan.Domain.Entities.Recipes;

public class RecipeIngredient : BaseEntity
{
    public int? RecipeId { get; set; }
    public int? IngredientId { get; set; }
    public string IngredientName { get; set; }

    public decimal? Quantity { get; set; }

    public string Unit { get; set; }

    public string PreparationNotes { get; set; }

    public bool? IsOptional { get; set; }

    public int? OrderInRecipe { get; set; }
    public virtual Ingredient Ingredient { get; set; }

    public virtual Recipe Recipe { get; set; }
}