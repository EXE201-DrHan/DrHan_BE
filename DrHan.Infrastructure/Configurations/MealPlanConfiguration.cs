using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.MealPlans;

public class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.HasIndex(mp => mp.UserId);
        builder.HasIndex(mp => mp.FamilyId);
    }
} 