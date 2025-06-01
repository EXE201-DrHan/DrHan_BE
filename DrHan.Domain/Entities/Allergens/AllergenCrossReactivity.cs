#nullable disable
namespace DrHan.Domain.Entities.Allergens;

public class AllergenCrossReactivity : BaseEntity
{
    public int? AllergenId { get; set; }
    public int? GroupId { get; set; }
    public virtual Allergen Allergen { get; set; }

    public virtual CrossReactivityGroup Group { get; set; }
}