#nullable disable
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Users;

namespace DrHan.Domain.Entities.FoodProducts;

public class FoodProduct : BaseEntity
{
    public string Barcode { get; set; }

    public string Name { get; set; }

    public string Brand { get; set; }

    public string Manufacturer { get; set; }

    public string ProductSize { get; set; }

    public string Category { get; set; }

    public string Subcategory { get; set; }

    public string FoodGroup { get; set; }

    public string ServingSize { get; set; }

    public int? CaloriesPerServing { get; set; }

    public string IngredientsList { get; set; }

    public string Description { get; set; }

    public string DataSource { get; set; }

    public decimal? DataQualityScore { get; set; }

    public int? VerifiedByUsers { get; set; }
    public bool? IsActive { get; set; }

    public virtual ICollection<MealPlanEntry> MealPlanEntries { get; set; } = new List<MealPlanEntry>();

    public virtual ICollection<ProductAllergenFreeClaim> ProductAllergenFreeClaims { get; set; } =
        new List<ProductAllergenFreeClaim>();

    public virtual ICollection<ProductAllergen> ProductAllergens { get; set; } = new List<ProductAllergen>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();

    public virtual ICollection<ProductNutrition> ProductNutritions { get; set; } = new List<ProductNutrition>();

    public virtual ICollection<ScanHistory> ScanHistories { get; set; } = new List<ScanHistory>();

    public virtual ICollection<UserActivity> UserActivities { get; set; } = new List<UserActivity>();
}