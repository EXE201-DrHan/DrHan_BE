using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Entities.MealPlans;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.MealPlanServices.Commands.CreateMealPlan;

public class CreateMealPlanCommandHandler : IRequestHandler<CreateMealPlanCommand, AppResponse<MealPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUserContext _userContext;
    private readonly ILogger<CreateMealPlanCommandHandler> _logger;

    public CreateMealPlanCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ILogger<CreateMealPlanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<AppResponse<MealPlanDto>> Handle(CreateMealPlanCommand request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<MealPlanDto>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();

            // Validate date range
            if (request.MealPlan.EndDate <= request.MealPlan.StartDate)
            {
                return response.SetErrorResponse("Dates", "End date must be after start date");
            }

            var mealPlan = new MealPlan
            {
                UserId = userId,
                FamilyId = request.MealPlan.FamilyId,
                Name = request.MealPlan.Name,
                StartDate = request.MealPlan.StartDate,
                EndDate = request.MealPlan.EndDate,
                PlanType = request.MealPlan.PlanType,
                Notes = request.MealPlan.Notes
            };

            await _unitOfWork.Repository<MealPlan>().AddAsync(mealPlan);
            await _unitOfWork.CompleteAsync();

            var mealPlanDto = _mapper.Map<MealPlanDto>(mealPlan);
            
            _logger.LogInformation("Meal plan {MealPlanId} created for user {UserId}", mealPlan.Id, userId);
            return response.SetSuccessResponse(mealPlanDto, "Success", "Meal plan created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meal plan for user {UserId}", _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while creating the meal plan");
        }
    }
} 