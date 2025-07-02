namespace DrHan.Application.DTOs.Recipes;

public class RecipeFilterOptionsDto
{
    public List<string> CuisineTypes { get; set; } = new();
    public List<string> MealTypes { get; set; } = new();
    public List<string> DifficultyLevels { get; set; } = new();
    public List<string> SortOptions { get; set; } = new();
    public List<string> AvailableAllergens { get; set; } = new();
    public List<string> AvailableAllergenFreeClaims { get; set; } = new();
    public List<string> AvailableIngredients { get; set; } = new();
    public List<string> IngredientCategories { get; set; } = new();
} 