#nullable disable
using DrHan.Domain.Entities.Users;

namespace DrHan.Domain.Entities.Allergens;

public class EmergencyMedication : BaseEntity
{
    public int? UserAllergyId { get; set; }
    public string MedicationName { get; set; }

    public string MedicationType { get; set; }

    public string Dosage { get; set; }

    public string Instructions { get; set; }
    public virtual UserAllergy UserAllergy { get; set; }
}