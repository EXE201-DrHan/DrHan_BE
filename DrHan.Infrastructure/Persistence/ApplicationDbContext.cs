using DrHan.Domain.Entities;
using DrHan.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DrHan.Domain.Entities.Allergens;
using DrHan.Domain.Entities.Blogs;
using DrHan.Domain.Entities.Families;
using DrHan.Domain.Entities.FoodProducts;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Recipes;
using DrHan.Domain.Entities.Notifications;

namespace DrHan.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public ApplicationDbContext(DbContextOptions options) : base(options) { }

    public virtual DbSet<Allergen> Allergens { get; set; }

    public virtual DbSet<AllergenCrossReactivity> AllergenCrossReactivities { get; set; }

    public virtual DbSet<AllergenName> AllergenNames { get; set; }

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<BlogImage> BlogImages { get; set; }

    public virtual DbSet<BlogTag> BlogTags { get; set; }

    public virtual DbSet<CrossReactivityGroup> CrossReactivityGroups { get; set; }

    public virtual DbSet<EmergencyMedication> EmergencyMedications { get; set; }

    public virtual DbSet<Family> Families { get; set; }

    public virtual DbSet<FamilyMember> FamilyMembers { get; set; }

    public virtual DbSet<FamilyMemberPermission> FamilyMemberPermissions { get; set; }

    public virtual DbSet<FoodProduct> FoodProducts { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<IngredientAllergen> IngredientAllergens { get; set; }

    public virtual DbSet<IngredientName> IngredientNames { get; set; }

    public virtual DbSet<IngredientNutrition> IngredientNutritions { get; set; }

    public virtual DbSet<MealPlan> MealPlans { get; set; }

    public virtual DbSet<MealPlanEntry> MealPlanEntries { get; set; }

    public virtual DbSet<MealPlanShoppingItem> MealPlanShoppingItems { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PlanFeature> PlanFeatures { get; set; }

    public virtual DbSet<ProductAllergen> ProductAllergens { get; set; }

    public virtual DbSet<ProductAllergenFreeClaim> ProductAllergenFreeClaims { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductIngredient> ProductIngredients { get; set; }

    public virtual DbSet<ProductNutrition> ProductNutritions { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<RecipeAllergen> RecipeAllergens { get; set; }

    public virtual DbSet<RecipeAllergenFreeClaim> RecipeAllergenFreeClaims { get; set; }

    public virtual DbSet<RecipeImage> RecipeImages { get; set; }

    public virtual DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    public virtual DbSet<RecipeInstruction> RecipeInstructions { get; set; }

    public virtual DbSet<RecipeNutrition> RecipeNutritions { get; set; }

    public virtual DbSet<ScanHistory> ScanHistories { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<SystemConfig> SystemConfigs { get; set; }

    public virtual DbSet<SystemConfigDetail> SystemConfigDetails { get; set; }

    public virtual DbSet<UserActivity> UserActivities { get; set; }

    public virtual DbSet<UserAllergy> UserAllergies { get; set; }

    public virtual DbSet<UserAllergySymptom> UserAllergySymptoms { get; set; }

    public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<UserOtp> UserOtps { get; set; }
    public virtual DbSet<UserDeviceToken> UserDeviceTokens { get; set; }
    public virtual DbSet<SubscriptionUsage> SubscriptionUsages { get; set; }
    public virtual DbSet<UserMealNotificationSettings> UserMealNotificationSettings { get; set; }
    public virtual DbSet<MealNotificationLog> MealNotificationLogs { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplyBaseEntityToDerivedClass(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Only configure if not already configured (don't override DI configuration)
        if (!optionsBuilder.IsConfigured)
        {
            // Configure query splitting to improve performance when loading multiple related collections
            optionsBuilder.UseSqlServer(o => 
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                o.CommandTimeout(60); // Increase timeout to 60 seconds for complex queries
            });
        }
    }

    /// <summary>
    /// Configuring the base entity
    /// </summary>
    /// <param name="modelBuilder"></param>
    private void ApplyBaseEntityToDerivedClass(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.ClrType != typeof(BaseEntity))
            {
                // Configure Id
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Id))
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                //Configure Guid
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.BusinessId))
                    .IsRequired()
                    .HasDefaultValueSql("NEWID()")
                    .ValueGeneratedOnAdd();

                // Configure CreatedAt
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.CreateAt))
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                // Configure UpdatedAt
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.UpdateAt))
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
                
            }
        }
    }
}