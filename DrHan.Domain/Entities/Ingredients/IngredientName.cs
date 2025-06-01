#nullable disable
namespace DrHan.Domain.Entities.Ingredients;

public class IngredientName : BaseEntity
{
    public int? IngredientId { get; set; }
    public string Name { get; set; }

    public bool? IsPrimary { get; set; }
    public virtual Ingredient Ingredient { get; set; }
}