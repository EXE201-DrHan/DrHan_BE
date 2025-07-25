using AutoMapper;
using DrHan.Application.DTOs.MealPlans;

namespace DrHan.Application.Automapper;

public class SmartScoringProfile : Profile
{
    public SmartScoringProfile()
    {
        // Map RecipeScore for API responses (if needed in the future)
        CreateMap<RecipeScore, RecipeScore>(); // Identity mapping for now
        
        // Map UserCuisinePreference for API responses
        CreateMap<UserCuisinePreference, UserCuisinePreference>(); // Identity mapping for now
        
        // Additional mappings can be added here as the system evolves
    }
} 