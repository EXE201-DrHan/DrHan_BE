namespace DrHan.Application.DTOs.MealPlans;

public class SmartSelectionContext
{
    public int UserId { get; set; }
    public string MealType { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly CurrentTime { get; set; }
    public List<int> RecentRecipeIds { get; set; } = new();
    public int TargetCalories { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsRushHour { get; set; }
    public MealPlanPreferencesDto? Preferences { get; set; }
}

public class RecipeScore
{
    public int RecipeId { get; set; }
    public double TotalScore { get; set; }
    public double QualityScore { get; set; }
    public double VarietyScore { get; set; }
    public double TimeScore { get; set; }
    public double NutritionalScore { get; set; }
    public double UserPreferenceScore { get; set; }
    public string ScoreBreakdown { get; set; }
}

public class UserCuisinePreference
{
    public string CuisineType { get; set; }
    public int UsageCount { get; set; }
    public double PreferenceRatio { get; set; }
    public double CompletionRate { get; set; }
}

public class NutritionalTarget
{
    public int TargetCalories { get; set; }
    public double TargetProtein { get; set; }
    public double TargetCarbs { get; set; }
    public double TargetFat { get; set; }
    
    public static NutritionalTarget GetMealTarget(string mealType, int dailyCalories = 2000)
    {
        return mealType switch
        {
            "Breakfast" => new NutritionalTarget
            {
                TargetCalories = (int)(dailyCalories * 0.25), // 25% of daily
                TargetProtein = 15,
                TargetCarbs = 30,
                TargetFat = 12
            },
            "Lunch" => new NutritionalTarget
            {
                TargetCalories = (int)(dailyCalories * 0.35), // 35% of daily
                TargetProtein = 25,
                TargetCarbs = 45,
                TargetFat = 18
            },
            "Dinner" => new NutritionalTarget
            {
                TargetCalories = (int)(dailyCalories * 0.40), // 40% of daily
                TargetProtein = 30,
                TargetCarbs = 50,
                TargetFat = 20
            },
            "Snack" => new NutritionalTarget
            {
                TargetCalories = (int)(dailyCalories * 0.10), // 10% of daily
                TargetProtein = 8,
                TargetCarbs = 15,
                TargetFat = 6
            },
            _ => new NutritionalTarget
            {
                TargetCalories = (int)(dailyCalories * 0.30),
                TargetProtein = 20,
                TargetCarbs = 35,
                TargetFat = 15
            }
        };
    }
} 