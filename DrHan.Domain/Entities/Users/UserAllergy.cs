#nullable disable
using DrHan.Domain.Entities.Allergens;

namespace DrHan.Domain.Entities.Users;

public class UserAllergy : BaseEntity
{
    public int? UserId { get; set; }
    public int? AllergenId { get; set; }
    public string Severity { get; set; }

    public DateOnly? DiagnosisDate { get; set; }

    public string DiagnosedBy { get; set; }

    public DateOnly? LastReactionDate { get; set; }

    public string AvoidanceNotes { get; set; }

    public bool? Outgrown { get; set; }

    public DateOnly? OutgrownDate { get; set; }

    public bool? NeedsVerification { get; set; }
    public virtual Allergen Allergen { get; set; }

    public virtual ICollection<EmergencyMedication> EmergencyMedications { get; set; } =
        new List<EmergencyMedication>();

    public virtual ApplicationUser ApplicationUser { get; set; }

    public virtual ICollection<UserAllergySymptom> UserAllergySymptoms { get; set; } = new List<UserAllergySymptom>();
}