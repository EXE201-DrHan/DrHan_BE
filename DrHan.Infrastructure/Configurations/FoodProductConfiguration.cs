using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.FoodProducts;

public class FoodProductConfiguration : IEntityTypeConfiguration<FoodProduct>
{
    public void Configure(EntityTypeBuilder<FoodProduct> builder)
    {
        builder.HasIndex(fp => fp.Barcode).IsUnique();
        builder.HasIndex(fp => fp.Name);
        builder.HasIndex(fp => fp.Brand);
        builder.HasIndex(fp => fp.IsActive);
    }
} 