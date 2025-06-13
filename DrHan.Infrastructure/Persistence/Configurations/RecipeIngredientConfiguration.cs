using DrHan.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrHan.Infrastructure.Persistence.Configurations;

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        // Performance indexes for ingredient search
        builder.HasIndex(ri => ri.IngredientName)
               .HasDatabaseName("IX_RecipeIngredients_IngredientName");

        builder.HasIndex(ri => ri.RecipeId)
               .HasDatabaseName("IX_RecipeIngredients_RecipeId");

        // Composite index for recipe-ingredient lookups
        builder.HasIndex(ri => new { ri.RecipeId, ri.OrderInRecipe })
               .HasDatabaseName("IX_RecipeIngredients_RecipeId_OrderInRecipe");

        // String properties configuration
        builder.Property(ri => ri.IngredientName)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(ri => ri.Unit)
               .HasMaxLength(50);

        builder.Property(ri => ri.PreparationNotes)
               .HasMaxLength(500);
    }
} 