using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.MealPlans;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
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

    public GetUserMealPlansQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserContext userContext,
        ILogger<GetUserMealPlansQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<AppResponse<PaginatedList<MealPlanDto>>> Handle(GetUserMealPlansQuery request, CancellationToken cancellationToken)
    {
        var response = new AppResponse<PaginatedList<MealPlanDto>>();

        try
        {
            var userId = _userContext.GetCurrentUserId().GetValueOrDefault();
            
            var mealPlans = await _unitOfWork.Repository<MealPlan>()
                .ListAsyncWithPaginated(
                    filter: mp => mp.UserId == userId,
                    orderBy: query => query.OrderByDescending(mp => mp.CreateAt),
                    includeProperties: query => query.Include(mp => mp.MealPlanEntries).ThenInclude(mpe => mpe.Recipe),
                    pagination: request.Pagination,
                    cancellationToken: cancellationToken
                );

            var mealPlanDtos = _mapper.Map<List<MealPlanDto>>(mealPlans.Items);
            
            var paginatedResult = new PaginatedList<MealPlanDto>(
                mealPlanDtos, 
                mealPlans.TotalCount, 
                request.Pagination.PageNumber, 
                request.Pagination.PageSize);

            return response.SetSuccessResponse(paginatedResult, "Success", "Meal plans retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plans for user {UserId}", _userContext.GetCurrentUserId());
            return response.SetErrorResponse("Error", "An error occurred while retrieving meal plans");
        }
    }
} 