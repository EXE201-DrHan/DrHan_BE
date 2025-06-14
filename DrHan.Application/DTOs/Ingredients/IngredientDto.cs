namespace DrHan.Application.DTOs.Ingredients;

public class IngredientDto
{
    public int Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; }
    public DateTime UpdateAt { get; set; }
    
    public List<IngredientNutritionDto> Nutritions { get; set; } = new();
    public List<IngredientNameDto> AlternativeNames { get; set; } = new();
    public List<IngredientAllergenDto> Allergens { get; set; } = new();
}

public class IngredientNutritionDto
{
    public int Id { get; set; }
    public string NutrientName { get; set; } = string.Empty;
    public decimal? AmountPer100g { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class IngredientNameDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool? IsPrimary { get; set; }
}

public class IngredientAllergenDto
{
    public int Id { get; set; }
    public int? AllergenId { get; set; }
    public string AllergenName { get; set; } = string.Empty;
    public string AllergenType { get; set; } = string.Empty;
} 