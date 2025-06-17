using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Entities.MealPlans;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.MealPlanServices.Queries.GetMealPlanById;

public class GetMealPlanByIdQueryHandler : IRequestHandler<GetMealPlanByIdQuery, AppResponse<MealPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUserContext _userContext;
    private readonly ILogger<GetMealPlanByIdQueryHandler> _logger;

    public GetMealPlanByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ILogger<GetMealPlanByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<AppResponse<MealPlanDto>> Handle(GetMealPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<MealPlanDto>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            
            var mealPlans = await _unitOfWork.Repository<MealPlan>()
                .ListAsync(
                    filter: mp => mp.Id == request.Id && mp.UserId == userId,
                    includeProperties: query => query
                        .Include(mp => mp.MealPlanEntries)
                        .ThenInclude(mpe => mpe.Recipe)
                        .Include(mp => mp.MealPlanEntries)
                        .ThenInclude(mpe => mpe.Product)
                );
            
            var mealPlan = mealPlans.FirstOrDefault();

            if (mealPlan == null)
            {
                return response.SetErrorResponse("NotFound", "Meal plan not found");
            }

            var mealPlanDto = _mapper.Map<MealPlanDto>(mealPlan);
            return response.SetSuccessResponse(mealPlanDto, "Success", "Meal plan retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plan {MealPlanId} for user {UserId}", request.Id, _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while retrieving the meal plan");
        }
    }
} 