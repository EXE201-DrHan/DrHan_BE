using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.Allergens;

public class AllergenConfiguration : IEntityTypeConfiguration<Allergen>
{
    public void Configure(EntityTypeBuilder<Allergen> builder)
    {
        builder.HasIndex(a => a.Name).IsUnique();
        builder.HasIndex(a => a.Category);
    }
} 