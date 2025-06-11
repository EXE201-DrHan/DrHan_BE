using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.RecipeServices.Queries.GetRecipeById;

public class GetRecipeByIdQueryHandler : IRequestHandler<GetRecipeByIdQuery, AppResponse<RecipeDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRecipeByIdQueryHandler> _logger;

    public GetRecipeByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetRecipeByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppResponse<RecipeDetailDto>> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var recipes = await _unitOfWork.Repository<Recipe>().ListAsync(
                filter: r => r.Id == request.Id,
                includeProperties: query => query
                    .Include(r => r.RecipeIngredients)
                    .Include(r => r.RecipeInstructions.OrderBy(ri => ri.StepNumber))
                    .Include(r => r.RecipeNutritions)
                    .Include(r => r.RecipeAllergens)
                    .Include(r => r.RecipeAllergenFreeClaims)
                    .Include(r => r.RecipeImages)
            );

            var recipe = recipes.FirstOrDefault();
            
            if (recipe == null)
            {
                return new AppResponse<RecipeDetailDto>()
                    .SetErrorResponse("NotFound", "Recipe not found");
            }

            var recipeDetailDto = _mapper.Map<RecipeDetailDto>(recipe);

            return new AppResponse<RecipeDetailDto>()
                .SetSuccessResponse(recipeDetailDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe with ID {RecipeId}", request.Id);
            return new AppResponse<RecipeDetailDto>()
                .SetErrorResponse("Error", "An error occurred while retrieving the recipe");
        }
    }
} 