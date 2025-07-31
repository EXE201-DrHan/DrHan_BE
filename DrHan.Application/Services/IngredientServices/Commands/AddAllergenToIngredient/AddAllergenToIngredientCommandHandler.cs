using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Allergens;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.IngredientServices.Commands.AddAllergenToIngredient;

public class AddAllergenToIngredientCommandHandler : IRequestHandler<AddAllergenToIngredientCommand, AppResponse<IngredientAllergenDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddAllergenToIngredientCommandHandler> _logger;

    public AddAllergenToIngredientCommandHandler(IUnitOfWork unitOfWork, ILogger<AddAllergenToIngredientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppResponse<IngredientAllergenDto>> Handle(AddAllergenToIngredientCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if ingredient exists
            var ingredient = await _unitOfWork.Repository<Ingredient>().GetEntityByIdAsync(request.IngredientId);
            if (ingredient == null)
            {
                return new AppResponse<IngredientAllergenDto>()
                    .SetErrorResponse("IngredientNotFound", $"Ingredient with ID {request.IngredientId} not found.");
            }

            // Check if allergen exists
            var allergen = await _unitOfWork.Repository<Allergen>().GetEntityByIdAsync(request.AllergenId);
            if (allergen == null)
            {
                return new AppResponse<IngredientAllergenDto>()
                    .SetErrorResponse("AllergenNotFound", $"Allergen with ID {request.AllergenId} not found.");
            }

            // Check if the allergen is already associated with this ingredient
            var existingIngredientAllergen = await _unitOfWork.Repository<IngredientAllergen>()
                .ListAsync(ia => ia.IngredientId == request.IngredientId && ia.AllergenId == request.AllergenId);

            if (existingIngredientAllergen.Any())
            {
                return new AppResponse<IngredientAllergenDto>()
                    .SetErrorResponse("DuplicateAllergen", $"Allergen '{allergen.Name}' is already associated with ingredient '{ingredient.Name}'.");
            }

            // Create new ingredient allergen relationship
            var ingredientAllergen = new IngredientAllergen
            {
                IngredientId = request.IngredientId,
                AllergenId = request.AllergenId,
                AllergenType = request.AllergenType
            };

            await _unitOfWork.Repository<IngredientAllergen>().AddAsync(ingredientAllergen);
            await _unitOfWork.CompleteAsync();

            // Return the created ingredient allergen DTO
            var result = new IngredientAllergenDto
            {
                Id = ingredientAllergen.Id,
                AllergenId = ingredientAllergen.AllergenId,
                AllergenName = allergen.Name,
                AllergenType = ingredientAllergen.AllergenType
            };

            return new AppResponse<IngredientAllergenDto>()
                .SetSuccessResponse(result, "Success", $"Allergen '{allergen.Name}' successfully added to ingredient '{ingredient.Name}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding allergen to ingredient");
            return new AppResponse<IngredientAllergenDto>()
                .SetErrorResponse("Error", $"Error adding allergen to ingredient: {ex.Message}");
        }
    }
} 