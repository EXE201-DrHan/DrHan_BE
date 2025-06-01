using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.Recipes;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.HasIndex(r => r.Name).IsUnique();
        builder.HasIndex(r => r.CuisineType);
        builder.HasIndex(r => r.MealType);
        builder.HasIndex(r => r.IsPublic);
    }
} 