using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Entities.MealPlans;
using DrHan.Domain.Entities.Recipes;
using DrHan.Domain.Entities.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.MealPlanServices.Commands.GenerateSmartMealPlan;

public class GenerateSmartMealPlanCommandHandler : IRequestHandler<GenerateSmartMealPlanCommand, AppResponse<MealPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUserContext _userContext;
    private readonly ISmartMealPlanService _smartMealPlanService;
    private readonly ILogger<GenerateSmartMealPlanCommandHandler> _logger;

    public GenerateSmartMealPlanCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ISmartMealPlanService smartMealPlanService,
        ILogger<GenerateSmartMealPlanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userContext = userContext;
        _smartMealPlanService = smartMealPlanService;
        _logger = logger;
    }

    public async Task<AppResponse<MealPlanDto>> Handle(GenerateSmartMealPlanCommand request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<MealPlanDto>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            
            // Skip user validation for now - focus on meal plan generation

            // Generate smart meal plan
            var generatedPlan = await _smartMealPlanService.GenerateSmartMealPlanAsync(request.Request, userId);
            
            if (!generatedPlan.IsSucceeded)
            {
                return response.SetErrorResponse("Generation", "Failed to generate meal plan");
            }

            _logger.LogInformation("Smart meal plan generated successfully for user {UserId}", userId);
            return response.SetSuccessResponse(generatedPlan.Data, "Success", "Smart meal plan generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart meal plan for user {UserId}", _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while generating the meal plan");
        }
    }
} 