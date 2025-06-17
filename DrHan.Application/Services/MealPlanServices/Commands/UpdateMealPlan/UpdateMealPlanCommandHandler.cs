using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Entities.MealPlans;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.MealPlanServices.Commands.UpdateMealPlan;

public class UpdateMealPlanCommandHandler : IRequestHandler<UpdateMealPlanCommand, AppResponse<MealPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUserContext _userContext;
    private readonly ILogger<UpdateMealPlanCommandHandler> _logger;

    public UpdateMealPlanCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ILogger<UpdateMealPlanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<AppResponse<MealPlanDto>> Handle(UpdateMealPlanCommand request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<MealPlanDto>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();

            var mealPlan = await _unitOfWork.Repository<MealPlan>()
                .FindAsync(mp => mp.Id == request.MealPlan.Id);

            if (mealPlan == null)
            {
                return response.SetErrorResponse("NotFound", "Meal plan not found");
            }

            if (mealPlan.UserId != userId)
            {
                return response.SetErrorResponse("Authorization", "You don't have permission to update this meal plan");
            }

            // Update properties
            mealPlan.Name = request.MealPlan.Name;
            mealPlan.StartDate = request.MealPlan.StartDate;
            mealPlan.EndDate = request.MealPlan.EndDate;
            mealPlan.PlanType = request.MealPlan.PlanType;
            mealPlan.Notes = request.MealPlan.Notes;
            mealPlan.UpdateAt = DateTime.UtcNow;

            _unitOfWork.Repository<MealPlan>().Update(mealPlan);
            await _unitOfWork.CompleteAsync();

            var mealPlanDto = _mapper.Map<MealPlanDto>(mealPlan);
            
            _logger.LogInformation("Meal plan {MealPlanId} updated for user {UserId}", request.MealPlan.Id, userId);
            return response.SetSuccessResponse(mealPlanDto, "Success", "Meal plan updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating meal plan {MealPlanId} for user {UserId}", request.MealPlan.Id, _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while updating the meal plan");
        }
    }
} 