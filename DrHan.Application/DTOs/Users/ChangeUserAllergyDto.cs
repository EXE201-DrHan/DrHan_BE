using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Users;

public class ChangeUserAllergyDto
{
    [Required]
    public int NewAllergenId { get; set; }
    
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