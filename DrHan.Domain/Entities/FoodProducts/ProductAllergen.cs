#nullable disable
using DrHan.Domain.Entities.Allergens;

namespace DrHan.Domain.Entities.FoodProducts;

public class ProductAllergen : BaseEntity
{
    public int? ProductId { get; set; }
    public int? AllergenId { get; set; }
    public string AllergenType { get; set; }
    public virtual Allergen Allergen { get; set; }

    public virtual FoodProduct Product { get; set; }
}