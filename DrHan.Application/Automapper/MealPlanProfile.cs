using AutoMapper;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Domain.Entities.MealPlans;

namespace DrHan.Application.Automapper;

public class MealPlanProfile : Profile
{
    public MealPlanProfile()
    {
        // MealPlan mappings
        CreateMap<MealPlan, MealPlanDto>()
            .ForMember(dest => dest.TotalMeals, opt => opt.MapFrom(src => src.MealPlanEntries != null ? src.MealPlanEntries.Count : 0))
            .ForMember(dest => dest.CompletedMeals, opt => opt.MapFrom(src => src.MealPlanEntries != null ? src.MealPlanEntries.Count(mpe => mpe.IsCompleted) : 0))
            .ForMember(dest => dest.MealEntries, opt => opt.MapFrom(src => src.MealPlanEntries ?? new List<MealPlanEntry>()));

        // MealPlanEntry mappings
        CreateMap<MealPlanEntry, MealEntryDto>()
            .ForMember(dest => dest.MealName, opt => opt.MapFrom(src => 
                src.Recipe != null ? src.Recipe.Name :
                src.Product != null ? src.Product.Name :
                src.CustomMealName))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted));

        // MealPlanShoppingItem mappings
        CreateMap<MealPlanShoppingItem, ShoppingItemDto>()
            .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => 
                src.Ingredient != null ? src.Ingredient.IngredientNames.FirstOrDefault().Name :
                src.IngredientName));
    }
}

 