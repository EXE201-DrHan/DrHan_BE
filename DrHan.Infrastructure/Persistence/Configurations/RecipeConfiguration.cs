using DrHan.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrHan.Infrastructure.Persistence.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        // Performance indexes for recipe search
        builder.HasIndex(r => r.Name)
               .HasDatabaseName("IX_Recipes_Name");

        builder.HasIndex(r => r.CuisineType)
               .HasDatabaseName("IX_Recipes_CuisineType");

        builder.HasIndex(r => r.MealType)
               .HasDatabaseName("IX_Recipes_MealType");

        builder.HasIndex(r => r.PrepTimeMinutes)
               .HasDatabaseName("IX_Recipes_PrepTimeMinutes");

        builder.HasIndex(r => r.IsPublic)
               .HasDatabaseName("IX_Recipes_IsPublic");

        // Composite index for common search combinations
        builder.HasIndex(r => new { r.CuisineType, r.MealType, r.IsPublic })
               .HasDatabaseName("IX_Recipes_CuisineType_MealType_IsPublic");

        // String properties configuration
        builder.Property(r => r.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(r => r.Description)
               .HasMaxLength(1000);

        builder.Property(r => r.CuisineType)
               .HasMaxLength(100);

        builder.Property(r => r.MealType)
               .HasMaxLength(50);

        builder.Property(r => r.DifficultyLevel)
               .HasMaxLength(50);

        builder.Property(r => r.SourceUrl)
               .HasMaxLength(500);

        builder.Property(r => r.OriginalAuthor)
               .HasMaxLength(200);
    }
} 