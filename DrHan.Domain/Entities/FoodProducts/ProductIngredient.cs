#nullable disable
using DrHan.Domain.Entities.Ingredients;

namespace DrHan.Domain.Entities.FoodProducts;

public class ProductIngredient : BaseEntity
{
    public int? ProductId { get; set; }
    public int? IngredientId { get; set; }
    public int? OrderInList { get; set; }

    public decimal? Percentage { get; set; }

    public bool? IsAllergen { get; set; }

    public virtual Ingredient Ingredient { get; set; }

    public virtual FoodProduct Product { get; set; }
}