#nullable disable
namespace DrHan.Domain.Entities.FoodProducts;

public class ProductAllergenFreeClaim : BaseEntity
{
    public int? ProductId { get; set; }
    public string Claim { get; set; }

    public bool? Verified { get; set; }
    public virtual FoodProduct Product { get; set; }
}