#nullable disable
using DrHan.Domain.Entities.MealPlans;

namespace DrHan.Domain.Entities.Recipes;

public class Recipe : BaseEntity
{
    public string Name { get; set; }

    public string Description { get; set; }

    public string CuisineType { get; set; }

    public string MealType { get; set; }

    public int? PrepTimeMinutes { get; set; }

    public int? CookTimeMinutes { get; set; }

    public int? Servings { get; set; }

    public string DifficultyLevel { get; set; }

    public bool? IsCustom { get; set; }

    public bool? IsPublic { get; set; }

    public string SourceUrl { get; set; }

    public string OriginalAuthor { get; set; }
    public int? Author { get; set; }
    public int? LikesCount { get; set; }

    public int? SavesCount { get; set; }

    public decimal? RatingAverage { get; set; }

    public int? RatingCount { get; set; }
    public virtual ICollection<MealPlanEntry> MealPlanEntries { get; set; } = new List<MealPlanEntry>();

    public virtual ICollection<RecipeAllergenFreeClaim> RecipeAllergenFreeClaims { get; set; } =
        new List<RecipeAllergenFreeClaim>();

    public virtual ICollection<RecipeAllergen> RecipeAllergens { get; set; } = new List<RecipeAllergen>();

    public virtual ICollection<RecipeImage> RecipeImages { get; set; } = new List<RecipeImage>();

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual ICollection<RecipeInstruction> RecipeInstructions { get; set; } = new List<RecipeInstruction>();

    public virtual ICollection<RecipeNutrition> RecipeNutritions { get; set; } = new List<RecipeNutrition>();
}