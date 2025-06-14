using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Recipes;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.IngredientServices.Commands.DeleteIngredient;

public class DeleteIngredientCommandHandler : IRequestHandler<DeleteIngredientCommand, AppResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteIngredientCommandHandler> _logger;

    public DeleteIngredientCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteIngredientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppResponse<bool>> Handle(DeleteIngredientCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ingredient = await _unitOfWork.Repository<Ingredient>().FindAsync(i => i.Id == request.Id);

            if (ingredient == null)
                return new AppResponse<bool>().SetErrorResponse("NotFound", "Ingredient not found");

            // Check for safety constraints (if ingredient is used in recipes)
            var isUsedInRecipes = await _unitOfWork.Repository<RecipeIngredient>()
                .ExistsAsync(ri => ri.IngredientId == request.Id);

            if (isUsedInRecipes)
                return new AppResponse<bool>()
                    .SetErrorResponse("Validation", "Cannot delete ingredient as it is used in existing recipes");

            // Delete related data first
            var nutritions = await _unitOfWork.Repository<IngredientNutrition>()
                .ListAsync(filter: n => n.IngredientId == ingredient.Id);
            _unitOfWork.Repository<IngredientNutrition>().DeleteRange(nutritions);

            var names = await _unitOfWork.Repository<IngredientName>()
                .ListAsync(filter: n => n.IngredientId == ingredient.Id);
            _unitOfWork.Repository<IngredientName>().DeleteRange(names);

            var allergens = await _unitOfWork.Repository<IngredientAllergen>()
                .ListAsync(filter: a => a.IngredientId == ingredient.Id);
            _unitOfWork.Repository<IngredientAllergen>().DeleteRange(allergens);

            // Delete the ingredient
            _unitOfWork.Repository<Ingredient>().Delete(ingredient);
            
            await _unitOfWork.CompleteAsync();

            return new AppResponse<bool>()
                .SetSuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ingredient with id {Id}", request.Id);
            return new AppResponse<bool>()
                .SetErrorResponse("DeleteIngredient", "An error occurred while deleting the ingredient");
        }
    }
} 