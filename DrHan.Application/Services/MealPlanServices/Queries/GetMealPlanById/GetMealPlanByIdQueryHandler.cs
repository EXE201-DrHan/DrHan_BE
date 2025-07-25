using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Interfaces.Services.CacheService;
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
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyService _cacheKeyService;

    // Cache expiration for individual meal plans
    private static readonly TimeSpan MealPlanCacheExpiration = TimeSpan.FromMinutes(15);

    public GetMealPlanByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ILogger<GetMealPlanByIdQueryHandler> logger,
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

    public async Task<AppResponse<MealPlanDto>> Handle(GetMealPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<MealPlanDto>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            //var cacheKey = _cacheKeyService.Custom("user", userId, "mealplan", request.Id);

            //var mealPlanDto = await _cacheService.GetAsync<MealPlanDto>(cacheKey, async () =>
            //{
            //    var mealPlans = await _unitOfWork.Repository<MealPlan>()
            //        .ListAsync(
            //            filter: mp => mp.Id == request.Id && mp.UserId == userId,
            //            includeProperties: query => query
            //                .Include(mp => mp.MealPlanEntries)
            //                .ThenInclude(mpe => mpe.Recipe)
            //                .Include(mp => mp.MealPlanEntries)
            //                .ThenInclude(mpe => mpe.Product)
            //        );

            //    var mealPlan = mealPlans.FirstOrDefault();
            //    return mealPlan != null ? _mapper.Map<MealPlanDto>(mealPlan) : null;
            //}, MealPlanCacheExpiration);

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
            var mealPlanDto = mealPlan != null ? _mapper.Map<MealPlanDto>(mealPlan) : null;
            if (mealPlanDto == null)
            {
                return response.SetErrorResponse("NotFound", "Meal plan not found");
            }

            return response.SetSuccessResponse(mealPlanDto, "Success", "Meal plan retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plan {MealPlanId} for user {UserId}", request.Id, _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while retrieving the meal plan");
        }
    }
} 