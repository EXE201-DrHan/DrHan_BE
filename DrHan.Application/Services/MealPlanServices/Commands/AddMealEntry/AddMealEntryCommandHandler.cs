using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Interfaces.Services.CacheService;
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Recipes;
using DrHan.Domain.Entities.FoodProducts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.MealPlanServices.Commands.AddMealEntry;

public class AddMealEntryCommandHandler : IRequestHandler<AddMealEntryCommand, AppResponse<MealEntryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUserContext _userContext;
    private readonly ILogger<AddMealEntryCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyService _cacheKeyService;

    public AddMealEntryCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ILogger<AddMealEntryCommandHandler> logger,
        ICacheService cacheService,
        ICacheKeyService cacheKeyService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userContext = userContext;
        _logger = logger;
        _cacheService = cacheService;
        _cacheKeyService = cacheKeyService;
    }

    public async Task<AppResponse<MealEntryDto>> Handle(AddMealEntryCommand request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<MealEntryDto>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();

            // Verify meal plan exists and belongs to user
            var mealPlan = await _unitOfWork.Repository<MealPlan>()
                .FindAsync(mp => mp.Id == request.MealEntry.MealPlanId);

            if (mealPlan == null)
            {
                return response.SetErrorResponse("MealPlan", "Meal plan not found");
            }

            if (mealPlan.UserId != userId && mealPlan.FamilyId == null)
            {
                return response.SetErrorResponse("Authorization", "You don't have permission to modify this meal plan");
            }

            // Validate meal entry data
            var validationResult = await ValidateMealEntry(request.MealEntry);
            if (!validationResult.IsValid)
            {
                return response.SetErrorResponse("Validation", validationResult.ErrorMessage);
            }

            var mealEntry = new MealPlanEntry
            {
                MealPlanId = request.MealEntry.MealPlanId,
                MealDate = request.MealEntry.MealDate,
                MealType = request.MealEntry.MealType,
                RecipeId = request.MealEntry.RecipeId,
                ProductId = request.MealEntry.ProductId,
                CustomMealName = request.MealEntry.CustomMealName,
                Servings = request.MealEntry.Servings,
                Notes = request.MealEntry.Notes
            };

            await _unitOfWork.Repository<MealPlanEntry>().AddAsync(mealEntry);
            await _unitOfWork.CompleteAsync();

            // Invalidate caches after adding meal entry
            await InvalidateMealPlanCacheAsync(request.MealEntry.MealPlanId, userId);

            // Load related data for response
            if (mealEntry.RecipeId.HasValue)
            {
                mealEntry.Recipe = await _unitOfWork.Repository<Recipe>()
                    .FindAsync(r => r.Id == mealEntry.RecipeId.Value);
            }

            var mealEntryDto = _mapper.Map<MealEntryDto>(mealEntry);
            
            _logger.LogInformation("Meal entry added to plan {MealPlanId} for user {UserId}", request.MealEntry.MealPlanId, userId);
            return response.SetSuccessResponse(mealEntryDto, "Success", "Meal entry added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding meal entry for user {UserId}", _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while adding the meal entry");
        }
    }

    private async Task<(bool IsValid, string ErrorMessage)> ValidateMealEntry(AddMealEntryDto mealEntry)
    {
        // Validate that exactly one meal source is provided
        var sourceCount = 0;
        if (mealEntry.RecipeId.HasValue) sourceCount++;
        if (mealEntry.ProductId.HasValue) sourceCount++;
        if (!string.IsNullOrEmpty(mealEntry.CustomMealName)) sourceCount++;

        if (sourceCount != 1)
        {
            return (false, "Exactly one meal source must be provided (Recipe, Product, or Custom Meal)");
        }

        // Validate recipe exists if provided
        if (mealEntry.RecipeId.HasValue)
        {
            var recipe = await _unitOfWork.Repository<Recipe>()
                .FindAsync(r => r.Id == mealEntry.RecipeId.Value);
            if (recipe == null)
            {
                return (false, "Recipe not found");
            }
        }

        // Validate product exists if provided
        if (mealEntry.ProductId.HasValue)
        {
            var product = await _unitOfWork.Repository<FoodProduct>()
                .FindAsync(p => p.Id == mealEntry.ProductId.Value);
            if (product == null)
            {
                return (false, "Product not found");
            }
        }

        return (true, string.Empty);
    }

    private async Task InvalidateMealPlanCacheAsync(int mealPlanId, int userId)
    {
        try
        {
            // Invalidate specific meal plan cache
            var mealPlanCacheKey = _cacheKeyService.Custom("user", userId, "mealplan", mealPlanId);
            await _cacheService.RemoveAsync(mealPlanCacheKey);

            // Invalidate user's meal plan list cache pattern
            var userMealPlansPattern = _cacheKeyService.Custom("user", userId, "mealplans", "*");
            await _cacheService.RemoveByPatternAsync(userMealPlansPattern);

            _logger.LogInformation("Invalidated meal plan cache for meal plan {MealPlanId} and user {UserId}", mealPlanId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate meal plan cache for meal plan {MealPlanId}", mealPlanId);
        }
    }
} 