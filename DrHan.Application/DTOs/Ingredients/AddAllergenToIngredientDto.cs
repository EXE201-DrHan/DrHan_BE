using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Ingredients;

public class AddAllergenToIngredientDto
{
    [Required]
    public int IngredientId { get; set; }
    
    [Required]
    public int AllergenId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string AllergenType { get; set; } = string.Empty;
} 