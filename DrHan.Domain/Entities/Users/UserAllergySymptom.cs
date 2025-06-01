#nullable disable
namespace DrHan.Domain.Entities.Users;

public class UserAllergySymptom : BaseEntity
{
    public int? UserAllergyId { get; set; }
    public string Symptom { get; set; }

    public string Severity { get; set; }
    public virtual UserAllergy UserAllergy { get; set; }
}