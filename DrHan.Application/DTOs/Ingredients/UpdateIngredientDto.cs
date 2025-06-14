using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Ingredients;

public class UpdateIngredientDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    public List<UpdateIngredientNutritionDto> Nutritions { get; set; } = new();
    public List<UpdateIngredientNameDto> AlternativeNames { get; set; } = new();
    public List<int> AllergenIds { get; set; } = new();
}

public class UpdateIngredientNutritionDto
{
    public int? Id { get; set; } // For existing records
    
    [Required]
    [MaxLength(100)]
    public string NutrientName { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue)]
    public decimal? AmountPer100g { get; set; }
    
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
}

public class UpdateIngredientNameDto
{
    public int? Id { get; set; } // For existing records
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public bool IsPrimary { get; set; } = false;
}

public class IngredientCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
} 