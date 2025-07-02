using System.ComponentModel.DataAnnotations;
using DrHan.Application.DTOs.Allergens;

namespace DrHan.Application.DTOs.Users;

public class UserAllergyDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AllergenId { get; set; }
    public string? Severity { get; set; }
    public DateOnly? DiagnosisDate { get; set; }
    public string? DiagnosedBy { get; set; }
    public DateOnly? LastReactionDate { get; set; }
    public string? AvoidanceNotes { get; set; }
    public bool? Outgrown { get; set; }
    public DateOnly? OutgrownDate { get; set; }
    public bool? NeedsVerification { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public AllergenDto? Allergen { get; set; }
}

public class CreateUserAllergyDto
{
    [Required]
    public int AllergenId { get; set; }
    
    [StringLength(50)]
    public string? Severity { get; set; }
    
    public DateOnly? DiagnosisDate { get; set; }
    
    [StringLength(100)]
    public string? DiagnosedBy { get; set; }
    
    public DateOnly? LastReactionDate { get; set; }
    
    [StringLength(1000)]
    public string? AvoidanceNotes { get; set; }
    
    public bool? Outgrown { get; set; }
    public DateOnly? OutgrownDate { get; set; }
    public bool? NeedsVerification { get; set; }
}

public class UpdateUserAllergyDto
{
    [StringLength(50)]
    public string? Severity { get; set; }
    
    public DateOnly? DiagnosisDate { get; set; }
    
    [StringLength(100)]
    public string? DiagnosedBy { get; set; }
    
    public DateOnly? LastReactionDate { get; set; }
    
    [StringLength(1000)]
    public string? AvoidanceNotes { get; set; }
    
    public bool? Outgrown { get; set; }
    public DateOnly? OutgrownDate { get; set; }
    public bool? NeedsVerification { get; set; }
}

public class UserAllergyProfileDto
{
    public int UserId { get; set; }
    public List<UserAllergyDto> Allergies { get; set; } = new();
    public int TotalAllergies { get; set; }
    public int SevereAllergies { get; set; }
    public int OutgrownAllergies { get; set; }
}

public class HasAllergiesResponseDto
{
    public bool HasAllergies { get; set; }
    public int AllergyCount { get; set; }
}

public class CreateMultipleUserAllergiesDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one allergen ID is required")]
    public List<int> AllergenIds { get; set; } = new();
    
    [StringLength(50)]
    public string? Severity { get; set; }
    
    public DateOnly? DiagnosisDate { get; set; }
    
    [StringLength(100)]
    public string? DiagnosedBy { get; set; }
    
    public DateOnly? LastReactionDate { get; set; }
    
    [StringLength(1000)]
    public string? AvoidanceNotes { get; set; }
    
    public bool? Outgrown { get; set; }
    public DateOnly? OutgrownDate { get; set; }
    public bool? NeedsVerification { get; set; }
}

public class BulkUserAllergyResponseDto
{
    public List<UserAllergyDto> SuccessfullyAdded { get; set; } = new();
    public List<BulkAllergyErrorDto> Errors { get; set; } = new();
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
}

public class BulkAllergyErrorDto
{
    public int AllergenId { get; set; }
    public string Error { get; set; } = string.Empty;
} 