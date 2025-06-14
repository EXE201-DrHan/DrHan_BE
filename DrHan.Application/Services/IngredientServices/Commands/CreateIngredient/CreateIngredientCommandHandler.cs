using MediatR;
using AutoMapper;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Ingredients;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.IngredientServices.Commands.CreateIngredient;

public class CreateIngredientCommandHandler : IRequestHandler<CreateIngredientCommand, AppResponse<IngredientDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIngredientCommandHandler> _logger;

    public CreateIngredientCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateIngredientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IngredientDto>> Handle(CreateIngredientCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ingredient = new Ingredient
            {
                Name = request.Name,
                Category = request.Category,
                Description = request.Description
            };

            await _unitOfWork.Repository<Ingredient>().AddAsync(ingredient);
            await _unitOfWork.CompleteAsync();

            // Add nutritions if provided
            if (request.Nutritions.Any())
            {
                var nutritions = request.Nutritions.Select(n => new IngredientNutrition
                {
                    IngredientId = ingredient.Id,
                    NutrientName = n.NutrientName,
                    AmountPer100g = n.AmountPer100g,
                    Unit = n.Unit
                }).ToList();

                await _unitOfWork.Repository<IngredientNutrition>().AddRangeAsync(nutritions);
            }

            // Add alternative names if provided
            if (request.AlternativeNames.Any())
            {
                var names = request.AlternativeNames.Select(n => new IngredientName
                {
                    IngredientId = ingredient.Id,
                    Name = n.Name,
                    IsPrimary = n.IsPrimary
                }).ToList();

                await _unitOfWork.Repository<IngredientName>().AddRangeAsync(names);
            }

            // Add allergens if provided
            if (request.AllergenIds.Any())
            {
                var allergens = request.AllergenIds.Select(id => new IngredientAllergen
                {
                    IngredientId = ingredient.Id,
                    AllergenId = id
                }).ToList();

                await _unitOfWork.Repository<IngredientAllergen>().AddRangeAsync(allergens);
            }

            await _unitOfWork.CompleteAsync();

            // Get the created ingredient with includes for return
            var createdIngredient = await _unitOfWork.Repository<Ingredient>().FindAsync(i => i.Id == ingredient.Id);
            var ingredientDto = _mapper.Map<IngredientDto>(createdIngredient);
            
            return new AppResponse<IngredientDto>()
                .SetSuccessResponse(ingredientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ingredient");
            return new AppResponse<IngredientDto>()
                .SetErrorResponse("CreateIngredient", "Failed to create ingredient");
        }
    }
} 