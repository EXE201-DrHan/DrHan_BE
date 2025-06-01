#nullable disable
namespace DrHan.Domain.Entities.Recipes;

public class RecipeAllergenFreeClaim : BaseEntity
{
    public int? RecipeId { get; set; }
    public string Claim { get; set; }
    public virtual Recipe Recipe { get; set; }
}