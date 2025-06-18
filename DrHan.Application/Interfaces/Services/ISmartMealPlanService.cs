using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Commons;
using DrHan.Domain.Entities.MealPlans;

namespace DrHan.Application.Interfaces.Services;

public interface ISmartMealPlanService
{
    Task<AppResponse<MealPlanDto>> GenerateSmartMealPlanAsync(GenerateMealPlanDto request, int userId);
    Task<AppResponse<MealPlanDto>> GenerateSmartMealsAsync(int mealPlanId, GenerateSmartMealsDto request, int userId);
    Task<AppResponse<List<int>>> GetRecommendedRecipesAsync(MealPlanPreferencesDto preferences, int userId, string mealType);
    Task<AppResponse<MealPlanDto>> ApplyTemplateAsync(int templateId, CreateMealPlanDto mealPlan, int userId);
    Task<AppResponse<bool>> BulkFillMealsAsync(BulkFillMealsDto request, int userId);
    Task<AppResponse<List<MealPlanTemplateDto>>> GetAvailableTemplatesAsync();
} 