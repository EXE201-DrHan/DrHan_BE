#nullable disable
namespace DrHan.Domain.Entities.FoodProducts;

public class ProductNutrition : BaseEntity
{
    public int? ProductId { get; set; }
    public string NutrientName { get; set; }

    public decimal? AmountPerServing { get; set; }

    public string Unit { get; set; }

    public decimal? DailyValuePercent { get; set; }
    public virtual FoodProduct Product { get; set; }
}