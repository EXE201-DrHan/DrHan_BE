using AutoMapper;
using DrHan.Application.DTOs.Recipes;
using DrHan.Domain.Entities.Recipes;

namespace DrHan.Application.Automapper;

public class RecipeProfile : Profile
{
    public RecipeProfile()
    {
        // Recipe mappings
        CreateMap<Recipe, RecipeDto>()
            .ForMember(dest => dest.ThumbnailImageUrl, opt => opt.MapFrom(src => 
                src.RecipeImages.Where(ri => ri.IsPrimary == true).FirstOrDefault() != null ? 
                src.RecipeImages.Where(ri => ri.IsPrimary == true).FirstOrDefault()!.ImageUrl :
                src.RecipeImages.FirstOrDefault() != null ? 
                src.RecipeImages.FirstOrDefault()!.ImageUrl : null));

        CreateMap<Recipe, RecipeDetailDto>()
            .ForMember(dest => dest.ThumbnailImageUrl, opt => opt.MapFrom(src => 
                src.RecipeImages.Where(ri => ri.IsPrimary == true).FirstOrDefault() != null ? 
                src.RecipeImages.Where(ri => ri.IsPrimary == true).FirstOrDefault()!.ImageUrl :
                src.RecipeImages.FirstOrDefault() != null ? 
                src.RecipeImages.FirstOrDefault()!.ImageUrl : null))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.RecipeIngredients))
            .ForMember(dest => dest.Instructions, opt => opt.MapFrom(src => src.RecipeInstructions))
            .ForMember(dest => dest.Nutrition, opt => opt.MapFrom(src => src.RecipeNutritions))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.RecipeImages))
            .ForMember(dest => dest.Allergens, opt => opt.MapFrom(src => src.RecipeAllergens.Select(ra => ra.AllergenType)))
            .ForMember(dest => dest.AllergenFreeClaims, opt => opt.MapFrom(src => src.RecipeAllergenFreeClaims.Select(rc => rc.Claim)));

        // Recipe component mappings
        CreateMap<RecipeIngredient, RecipeIngredientDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.IngredientName))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.PreparationNotes))
            .ForMember(dest => dest.OrderIndex, opt => opt.MapFrom(src => src.OrderInRecipe ?? 0));

        CreateMap<RecipeInstruction, RecipeInstructionDto>()
            .ForMember(dest => dest.Instruction, opt => opt.MapFrom(src => src.InstructionText))
            .ForMember(dest => dest.EstimatedTimeMinutes, opt => opt.MapFrom(src => src.TimeMinutes));

        CreateMap<RecipeNutrition, RecipeNutritionDto>()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.AmountPerServing))
            .ForMember(dest => dest.DailyValuePercentage, opt => opt.MapFrom(src => src.DailyValuePercent));

        CreateMap<RecipeImage, RecipeImageDto>();

        // Reverse mappings for creating entities from DTOs
        CreateMap<RecipeIngredientDto, RecipeIngredient>()
            .ForMember(dest => dest.IngredientName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.PreparationNotes, opt => opt.MapFrom(src => src.Notes))
            .ForMember(dest => dest.OrderInRecipe, opt => opt.MapFrom(src => src.OrderIndex))
            .ForMember(dest => dest.BusinessId, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<RecipeInstructionDto, RecipeInstruction>()
            .ForMember(dest => dest.InstructionText, opt => opt.MapFrom(src => src.Instruction))
            .ForMember(dest => dest.TimeMinutes, opt => opt.MapFrom(src => src.EstimatedTimeMinutes))
            .ForMember(dest => dest.BusinessId, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<RecipeNutritionDto, RecipeNutrition>()
            .ForMember(dest => dest.AmountPerServing, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.DailyValuePercent, opt => opt.MapFrom(src => src.DailyValuePercentage))
            .ForMember(dest => dest.BusinessId, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<RecipeImageDto, RecipeImage>()
            .ForMember(dest => dest.BusinessId, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
} 