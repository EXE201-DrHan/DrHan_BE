#nullable disable
namespace DrHan.Domain.Entities.Recipes;

public class RecipeInstruction : BaseEntity
{
    public int? RecipeId { get; set; }
    public int StepNumber { get; set; }

    public string InstructionText { get; set; }

    public int? TimeMinutes { get; set; }

    public int? Temperature { get; set; }
    public virtual Recipe Recipe { get; set; }
}