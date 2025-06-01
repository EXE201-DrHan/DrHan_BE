#nullable disable
using DrHan.Domain.Entities.Allergens;

namespace DrHan.Domain.Entities.Ingredients;

public class IngredientAllergen : BaseEntity
{
    public int? IngredientId { get; set; }
    public int? AllergenId { get; set; }
    public string AllergenType { get; set; }

    public virtual Allergen Allergen { get; set; }

    public virtual Ingredient Ingredient { get; set; }
}