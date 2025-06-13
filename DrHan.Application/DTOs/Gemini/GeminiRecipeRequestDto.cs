using System.Collections.Generic;

namespace DrHan.Application.DTOs.Gemini;

public class GeminiRecipeRequestDto
{
    public string SearchQuery { get; set; } = string.Empty;
    public string? CuisineType { get; set; }
    public string? MealType { get; set; }
    public string? DifficultyLevel { get; set; }
    public int? MaxPrepTime { get; set; }
    public int? MaxCookTime { get; set; }
    public int? Servings { get; set; }
    public List<string>? ExcludeAllergens { get; set; }
    public int Count { get; set; } = 5; // Number of recipes to fetch
    public bool IncludeImage { get; set; } = true; // Request recipe images
} 