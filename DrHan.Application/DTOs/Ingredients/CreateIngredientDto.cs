using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Ingredients;

public class CreateIngredientDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    public List<CreateIngredientNutritionDto> Nutritions { get; set; } = new();
    public List<CreateIngredientNameDto> AlternativeNames { get; set; } = new();
    public List<int> AllergenIds { get; set; } = new();
}

public class CreateIngredientNutritionDto
{
    [Required]
    [MaxLength(100)]
    public string NutrientName { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue)]
    public decimal? AmountPer100g { get; set; }
    
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
}

public class CreateIngredientNameDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public bool IsPrimary { get; set; } = false;
} 