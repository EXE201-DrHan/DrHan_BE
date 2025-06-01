#nullable disable
using DrHan.Domain.Entities.FoodProducts;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Recipes;
using DrHan.Domain.Entities.Users;

namespace DrHan.Domain.Entities.Allergens;

public class Allergen : BaseEntity
{
    public string Name { get; set; }

    public string Category { get; set; }

    public string ScientificName { get; set; }

    public string Description { get; set; }

    public bool? IsFdaMajor { get; set; }

    public bool? IsEuMajor { get; set; }

    public virtual ICollection<AllergenCrossReactivity> AllergenCrossReactivities { get; set; } =
        new List<AllergenCrossReactivity>();

    public virtual ICollection<AllergenName> AllergenNames { get; set; } = new List<AllergenName>();

    public virtual ICollection<IngredientAllergen> IngredientAllergens { get; set; } = new List<IngredientAllergen>();

    public virtual ICollection<ProductAllergen> ProductAllergens { get; set; } = new List<ProductAllergen>();

    public virtual ICollection<RecipeAllergen> RecipeAllergens { get; set; } = new List<RecipeAllergen>();

    public virtual ICollection<UserAllergy> UserAllergies { get; set; } = new List<UserAllergy>();
}