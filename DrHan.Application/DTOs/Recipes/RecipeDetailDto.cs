namespace DrHan.Application.DTOs.Recipes;

public class RecipeDetailDto : RecipeDto
{
    public string SourceUrl { get; set; } = string.Empty;
    public string OriginalAuthor { get; set; } = string.Empty;
    public int? Author { get; set; }
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
    public List<RecipeInstructionDto> Instructions { get; set; } = new();
    public List<RecipeNutritionDto> Nutrition { get; set; } = new();
    public List<string> Allergens { get; set; } = new();
    public List<string> AllergenFreeClaims { get; set; } = new();
    public List<RecipeImageDto> Images { get; set; } = new();
}

public class RecipeIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int OrderIndex { get; set; }
}

public class RecipeInstructionDto
{
    public int StepNumber { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public int? EstimatedTimeMinutes { get; set; }
}

public class RecipeNutritionDto
{
    public string NutrientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? DailyValuePercentage { get; set; }
}

public class RecipeImageDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? ImageDescription { get; set; }
    public bool IsPrimary { get; set; }
    public int OrderIndex { get; set; }
} 