using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.IngredientServices.Commands.UpdateIngredient;

public class UpdateIngredientCommandHandler : IRequestHandler<UpdateIngredientCommand, AppResponse<IngredientDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateIngredientCommandHandler> _logger;

    public UpdateIngredientCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateIngredientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IngredientDto>> Handle(UpdateIngredientCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ingredient = await _unitOfWork.Repository<Ingredient>().FindAsync(i => i.Id == request.Id);

            if (ingredient == null)
                return new AppResponse<IngredientDto>().SetErrorResponse("Ingredient", "Ingredient not found");

            // Update basic properties
            if (!string.IsNullOrEmpty(request.Name))
                ingredient.Name = request.Name;
            
            if (!string.IsNullOrEmpty(request.Category))
                ingredient.Category = request.Category;
            
            if (!string.IsNullOrEmpty(request.Description))
                ingredient.Description = request.Description;

            ingredient.UpdateAt = DateTime.UtcNow;
            _unitOfWork.Repository<Ingredient>().Update(ingredient);

            // Update nutritions if provided
            if (request.Nutritions != null)
            {
                // Remove existing nutritions
                var existingNutritions = await _unitOfWork.Repository<IngredientNutrition>()
                    .ListAsync(filter: n => n.IngredientId == ingredient.Id);
                _unitOfWork.Repository<IngredientNutrition>().DeleteRange(existingNutritions);

                // Add new nutritions
                var nutritions = request.Nutritions.Select(n => new IngredientNutrition
                {
                    IngredientId = ingredient.Id,
                    NutrientName = n.NutrientName,
                    AmountPer100g = n.AmountPer100g,
                    Unit = n.Unit
                }).ToList();

                await _unitOfWork.Repository<IngredientNutrition>().AddRangeAsync(nutritions);
            }

            // Update alternative names if provided
            if (request.AlternativeNames != null)
            {
                // Remove existing names
                var existingNames = await _unitOfWork.Repository<IngredientName>()
                    .ListAsync(filter: n => n.IngredientId == ingredient.Id);
                _unitOfWork.Repository<IngredientName>().DeleteRange(existingNames);

                // Add new names
                var names = request.AlternativeNames.Select(n => new IngredientName
                {
                    IngredientId = ingredient.Id,
                    Name = n.Name,
                    IsPrimary = n.IsPrimary
                }).ToList();

                await _unitOfWork.Repository<IngredientName>().AddRangeAsync(names);
            }

            // Update allergens if provided
            if (request.AllergenIds != null)
            {
                // Remove existing allergens
                var existingAllergens = await _unitOfWork.Repository<IngredientAllergen>()
                    .ListAsync(filter: a => a.IngredientId == ingredient.Id);
                _unitOfWork.Repository<IngredientAllergen>().DeleteRange(existingAllergens);

                // Add new allergens
                var allergens = request.AllergenIds.Select(id => new IngredientAllergen
                {
                    IngredientId = ingredient.Id,
                    AllergenId = id
                }).ToList();

                await _unitOfWork.Repository<IngredientAllergen>().AddRangeAsync(allergens);
            }

            await _unitOfWork.CompleteAsync();

            // Get updated ingredient and return
            var updatedIngredient = await _unitOfWork.Repository<Ingredient>().FindAsync(i => i.Id == ingredient.Id);
            var ingredientDto = _mapper.Map<IngredientDto>(updatedIngredient);
            
            return new AppResponse<IngredientDto>()
                .SetSuccessResponse(ingredientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ingredient with id {Id}", request.Id);
            return new AppResponse<IngredientDto>()
                .SetErrorResponse("UpdateIngredient", "An error occurred while updating the ingredient");
        }
    }
} 