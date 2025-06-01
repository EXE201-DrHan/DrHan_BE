#nullable disable
namespace DrHan.Domain.Entities.Recipes;

public class RecipeImage : BaseEntity
{
    public int? RecipeId { get; set; }
    public string ImageUrl { get; set; }

    public string ImageType { get; set; }

    public int? StepNumber { get; set; }

    public bool? IsPrimary { get; set; }
    public virtual Recipe Recipe { get; set; }
}