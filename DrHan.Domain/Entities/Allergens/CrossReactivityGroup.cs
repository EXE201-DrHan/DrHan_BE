#nullable disable
namespace DrHan.Domain.Entities.Allergens;

public class CrossReactivityGroup : BaseEntity
{
    public string Name { get; set; }

    public string Description { get; set; }

    public virtual ICollection<AllergenCrossReactivity> AllergenCrossReactivities { get; set; } =
        new List<AllergenCrossReactivity>();
}