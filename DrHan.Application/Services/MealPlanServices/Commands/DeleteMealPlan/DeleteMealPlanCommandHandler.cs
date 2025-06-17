using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Interfaces.Services.CacheService;
using DrHan.Domain.Entities.MealPlans;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.MealPlanServices.Commands.DeleteMealPlan;

public class DeleteMealPlanCommandHandler : IRequestHandler<DeleteMealPlanCommand, AppResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;
    private readonly ILogger<DeleteMealPlanCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyService _cacheKeyService;

    public DeleteMealPlanCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContext userContext,
        ILogger<DeleteMealPlanCommandHandler> logger,
        ICacheService cacheService,
        ICacheKeyService cacheKeyService)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
        _logger = logger;
        _cacheService = cacheService;
        _cacheKeyService = cacheKeyService;
    }

    public async Task<AppResponse<bool>> Handle(DeleteMealPlanCommand request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<bool>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();

            var mealPlan = await _unitOfWork.Repository<MealPlan>()
                .FindAsync(mp => mp.Id == request.Id);

            if (mealPlan == null)
            {
                return response.SetErrorResponse("NotFound", "Meal plan not found");
            }

            if (mealPlan.UserId != userId)
            {
                return response.SetErrorResponse("Authorization", "You don't have permission to delete this meal plan");
            }

            // Delete related meal entries first
            var mealEntries = await _unitOfWork.Repository<MealPlanEntry>()
                .ListAsync(filter: mpe => mpe.MealPlanId == request.Id);

            foreach (var entry in mealEntries)
            {
                _unitOfWork.Repository<MealPlanEntry>().Delete(entry);
            }

            // Delete shopping items
            var shoppingItems = await _unitOfWork.Repository<MealPlanShoppingItem>()
                .ListAsync(filter: mpsi => mpsi.MealPlanId == request.Id);

            foreach (var item in shoppingItems)
            {
                _unitOfWork.Repository<MealPlanShoppingItem>().Delete(item);
            }

            // Delete the meal plan
            _unitOfWork.Repository<MealPlan>().Delete(mealPlan);
            await _unitOfWork.CompleteAsync();

            // Invalidate caches after deletion
            await InvalidateMealPlanCacheAsync(request.Id, userId);

            _logger.LogInformation("Meal plan {MealPlanId} deleted for user {UserId}", request.Id, userId);
            return response.SetSuccessResponse(true, "Success", "Meal plan deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meal plan {MealPlanId} for user {UserId}", request.Id, _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while deleting the meal plan");
        }
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