using DrHan.Application.Commons;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Entities.MealPlans;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.MealPlanServices.Commands.DeleteMealPlan;

public class DeleteMealPlanCommandHandler : IRequestHandler<DeleteMealPlanCommand, AppResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;
    private readonly ILogger<DeleteMealPlanCommandHandler> _logger;

    public DeleteMealPlanCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContext userContext,
        ILogger<DeleteMealPlanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
        _logger = logger;
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

            _logger.LogInformation("Meal plan {MealPlanId} deleted for user {UserId}", request.Id, userId);
            return response.SetSuccessResponse(true, "Success", "Meal plan deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meal plan {MealPlanId} for user {UserId}", request.Id, _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while deleting the meal plan");
        }
    }
} 