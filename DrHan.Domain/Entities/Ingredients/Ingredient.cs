#nullable disable
using DrHan.Domain.Entities.FoodProducts;
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Recipes;

namespace DrHan.Domain.Entities.Ingredients;

public class Ingredient : BaseEntity
{
    public string Name { get; set; }

    public string Category { get; set; }

    public string Description { get; set; }
    public virtual ICollection<IngredientAllergen> IngredientAllergens { get; set; } = new List<IngredientAllergen>();

    public virtual ICollection<IngredientName> IngredientNames { get; set; } = new List<IngredientName>();

    public virtual ICollection<IngredientNutrition> IngredientNutritions { get; set; } =
        new List<IngredientNutrition>();

    public virtual ICollection<MealPlanShoppingItem> MealPlanShoppingItems { get; set; } =
        new List<MealPlanShoppingItem>();

    public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}