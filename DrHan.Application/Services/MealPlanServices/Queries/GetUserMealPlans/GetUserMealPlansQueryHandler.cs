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

namespace DrHan.Application.Services.MealPlanServices.Queries.GetUserMealPlans;

public class GetUserMealPlansQueryHandler : IRequestHandler<GetUserMealPlansQuery, AppResponse<PaginatedList<MealPlanDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUserContext _userContext;
    private readonly ILogger<GetUserMealPlansQueryHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyService _cacheKeyService;

    // Cache expiration for user meal plan lists
    private static readonly TimeSpan UserMealPlansCacheExpiration = TimeSpan.FromMinutes(10);

    public GetUserMealPlansQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ILogger<GetUserMealPlansQueryHandler> logger,
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

    public async Task<AppResponse<PaginatedList<MealPlanDto>>> Handle(GetUserMealPlansQuery request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<PaginatedList<MealPlanDto>>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            //var cacheKey = _cacheKeyService.Custom("user", userId, "mealplans", "page", request.Pagination.PageNumber, "size", request.Pagination.PageSize);

            //var paginatedResult = await _cacheService.GetAsync<PaginatedList<MealPlanDto>>(cacheKey, async () =>
            //{
            //    var mealPlans = await _unitOfWork.Repository<MealPlan>()
            //        .ListAsyncWithPaginated(
            //            filter: mp => mp.UserId == userId,
            //            orderBy: query => query.OrderByDescending(mp => mp.CreateAt),
            //            includeProperties: query => query.Include(mp => mp.MealPlanEntries).ThenInclude(mpe => mpe.Recipe),
            //            pagination: request.Pagination,
            //            cancellationToken: cancellationToken
            //        );

            //    var mealPlanDtos = _mapper.Map<List<MealPlanDto>>(mealPlans.Items);

            //    return new PaginatedList<MealPlanDto>(
            //        mealPlanDtos, 
            //        mealPlans.TotalCount, 
            //        request.Pagination.PageNumber, 
            //        request.Pagination.PageSize);
            //}, UserMealPlansCacheExpiration);

            var paginatedResult = await _unitOfWork.Repository<MealPlan>()
                    .ListAsync(
                        filter: mp => mp.UserId == userId,
                        orderBy: query => query.OrderByDescending(mp => mp.CreateAt),
                        includeProperties: query => query.Include(mp => mp.MealPlanEntries).ThenInclude(mpe => mpe.Recipe)
                    );

            var mealPlanDtos = _mapper.Map<List<MealPlanDto>>(paginatedResult);

            var paginated =  new PaginatedList<MealPlanDto>(
                mealPlanDtos,
                paginatedResult.Count(),
                request.Pagination.PageNumber,
                request.Pagination.PageSize);
            return response.SetSuccessResponse(paginated, "Success", "Meal plans retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plans for user {UserId}", _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while retrieving meal plans");
        }
    }
} 