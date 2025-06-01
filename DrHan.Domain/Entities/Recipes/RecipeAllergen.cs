#nullable disable
using DrHan.Domain.Entities.Allergens;

namespace DrHan.Domain.Entities.Recipes;

public class RecipeAllergen : BaseEntity
{
    public int? RecipeId { get; set; }
    public int? AllergenId { get; set; }
    public string AllergenType { get; set; }

    public virtual Allergen Allergen { get; set; }

    public virtual Recipe Recipe { get; set; }
}