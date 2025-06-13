namespace DrHan.Application.DTOs.Gemini;

public class GeminiRecipeResponseDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public string MealType { get; set; } = string.Empty;
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string DifficultyLevel { get; set; } = string.Empty;
    public List<GeminiIngredientDto> Ingredients { get; set; } = new();
    public List<GeminiInstructionDto> Instructions { get; set; } = new();
    public List<string> Allergens { get; set; } = new();
    public List<string> AllergenFreeClaims { get; set; } = new();
    public string? ImageUrl { get; set; } // URL of the recipe image
}

public class GeminiIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class GeminiInstructionDto
{
    public int StepNumber { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public int? EstimatedTimeMinutes { get; set; }
}

// Models for Gemini API Response parsing
public class GeminiApiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiPart
{
    public string? Text { get; set; }
} 