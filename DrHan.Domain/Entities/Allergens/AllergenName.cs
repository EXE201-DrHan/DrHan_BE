#nullable disable
namespace DrHan.Domain.Entities.Allergens;

public class AllergenName : BaseEntity
{
    public int? AllergenId { get; set; }
    public string Name { get; set; }

    public bool? IsPrimary { get; set; }
    public virtual Allergen Allergen { get; set; }
}