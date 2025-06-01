#nullable disable
namespace DrHan.Domain.Entities.FoodProducts;

public class ProductImage : BaseEntity
{
    public int? ProductId { get; set; }
    public string ImageUrl { get; set; }

    public string ImageType { get; set; }

    public bool? IsPrimary { get; set; }
    public virtual FoodProduct Product { get; set; }
}