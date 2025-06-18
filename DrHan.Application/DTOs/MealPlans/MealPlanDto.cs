using DrHan.Application.DTOs.Recipes;

namespace DrHan.Application.DTOs.MealPlans;

public class MealPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string PlanType { get; set; }
    public string Notes { get; set; }
    public int TotalMeals { get; set; }
    public int CompletedMeals { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MealEntryDto> MealEntries { get; set; } = new();
}

public class CreateMealPlanDto
{
    public string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string PlanType { get; set; } // "Personal", "Family", "Weekly", "Monthly"
    public int? FamilyId { get; set; }
    public string Notes { get; set; }
}

public class UpdateMealPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string PlanType { get; set; }
    public string Notes { get; set; }
}

// Smart Generation DTOs
public class GenerateMealPlanDto
{
    public string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string PlanType { get; set; }
    public int? FamilyId { get; set; }
    public MealPlanPreferencesDto Preferences { get; set; }
}

public class MealPlanPreferencesDto
{
    public List<string> CuisineTypes { get; set; } = new(); // "Italian", "Asian", "Mexican"
    public int? MaxCookingTime { get; set; } // in minutes
    public string BudgetRange { get; set; } // "low", "medium", "high"
    public List<string> DietaryGoals { get; set; } = new(); // "balanced", "protein-rich", "low-carb"
    public string MealComplexity { get; set; } // "simple", "moderate", "complex"
    public List<string> PreferredMealTypes { get; set; } = new(); // "breakfast", "lunch", "dinner", "snack"
    public bool IncludeLeftovers { get; set; } = true;
    public bool VarietyMode { get; set; } = true; // Ensure meal variety
}

// Manual meal entry DTOs
public class AddMealEntryDto
{
    public int MealPlanId { get; set; }
    public DateOnly MealDate { get; set; }
    public string MealType { get; set; } // "Breakfast", "Lunch", "Dinner", "Snack"
    public int? RecipeId { get; set; }
    public int? ProductId { get; set; }
    public string CustomMealName { get; set; }
    public decimal? Servings { get; set; }
    public string Notes { get; set; }
}

public class UpdateMealEntryDto
{
    public int Id { get; set; }
    public DateOnly MealDate { get; set; }
    public string MealType { get; set; }
    public int? RecipeId { get; set; }
    public int? ProductId { get; set; }
    public string CustomMealName { get; set; }
    public decimal? Servings { get; set; }
    public string Notes { get; set; }
}

public class MealEntryDto
{
    public int Id { get; set; }
    public DateOnly MealDate { get; set; }
    public string MealType { get; set; }
    public string MealName { get; set; } // Recipe name, product name, or custom name
    public decimal? Servings { get; set; }
    public string Notes { get; set; }
    public int? RecipeId { get; set; }
    public int? ProductId { get; set; }
    public RecipeDto Recipe { get; set; } // If it's a recipe
    public bool IsCompleted { get; set; }
}

// Bulk operations
public class BulkFillMealsDto
{
    public int MealPlanId { get; set; }
    public string MealType { get; set; } // "breakfast", "lunch", "dinner", "snack" or "all"
    public string FillPattern { get; set; } // "rotate", "random", "same"
    public List<int> RecipeIds { get; set; } = new();
    public List<DateOnly> TargetDates { get; set; } = new(); // If empty, applies to all dates
}

public class MealPlanTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public int Duration { get; set; } // Days
    public Dictionary<string, List<int>> MealStructure { get; set; } = new(); // MealType -> RecipeIds
}

// Smart Meals Generation for Existing Meal Plan
public class GenerateSmartMealsDto
{
    public MealPlanPreferencesDto Preferences { get; set; }
    public List<DateOnly> TargetDates { get; set; } = new(); // If empty, applies to all meal plan dates
    public List<string> MealTypes { get; set; } = new(); // If empty, generates all meal types (breakfast, lunch, dinner)
    public bool ReplaceExisting { get; set; } = false; // Whether to replace existing meals or only fill empty slots
    public bool PreserveFavorites { get; set; } = true; // Don't replace meals marked as favorites
} 