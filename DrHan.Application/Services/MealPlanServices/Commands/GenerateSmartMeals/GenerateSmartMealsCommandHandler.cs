using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Entities.MealPlans;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.MealPlanServices.Commands.GenerateSmartMeals;

public class GenerateSmartMealsCommandHandler : IRequestHandler<GenerateSmartMealsCommand, AppResponse<MealPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUserContext _userContext;
    private readonly ISmartMealPlanService _smartMealPlanService;
    private readonly ILogger<GenerateSmartMealsCommandHandler> _logger;

    public GenerateSmartMealsCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ISmartMealPlanService smartMealPlanService,
        ILogger<GenerateSmartMealsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userContext = userContext;
        _smartMealPlanService = smartMealPlanService;
        _logger = logger;
    }

    public async Task<AppResponse<MealPlanDto>> Handle(GenerateSmartMealsCommand request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<MealPlanDto>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            
            // Verify meal plan exists and belongs to user
            var mealPlan = await _unitOfWork.Repository<MealPlan>()
                .GetEntityByIdAsync(request.MealPlanId);

            if (mealPlan == null)
            {
                return response.SetErrorResponse("NotFound", "Meal plan not found");
            }

            if (mealPlan.UserId != userId)
            {
                return response.SetErrorResponse("Authorization", "You don't have permission to modify this meal plan");
            }

            // Generate smart meals for the existing meal plan
            var result = await _smartMealPlanService.GenerateSmartMealsAsync(request.MealPlanId, request.Request, userId);
            
            if (!result.IsSucceeded)
            {
                return response.SetErrorResponse("Generation", result.Messages.FirstOrDefault().Value?.FirstOrDefault() ?? "Failed to generate meals");
            }

            _logger.LogInformation("Smart meals generated successfully for meal plan {MealPlanId} by user {UserId}", 
                request.MealPlanId, userId);
                
            return response.SetSuccessResponse(result.Data, "Success", "Smart meals generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart meals for meal plan {MealPlanId} by user {UserId}", 
                request.MealPlanId, _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while generating meals");
        }
    }
} 