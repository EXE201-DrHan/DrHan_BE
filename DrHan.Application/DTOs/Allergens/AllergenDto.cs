using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Allergens;

public class AllergenDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string? ScientificName { get; set; }
    public string? Description { get; set; }
    public bool? IsFdaMajor { get; set; }
    public bool? IsEuMajor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAllergenDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; }
    
    [StringLength(100)]
    public string? ScientificName { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public bool? IsFdaMajor { get; set; }
    public bool? IsEuMajor { get; set; }
}

public class UpdateAllergenDto
{
    [StringLength(100)]
    public string? Name { get; set; }
    
    [StringLength(50)]
    public string? Category { get; set; }
    
    [StringLength(100)]
    public string? ScientificName { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public bool? IsFdaMajor { get; set; }
    public bool? IsEuMajor { get; set; }
} 