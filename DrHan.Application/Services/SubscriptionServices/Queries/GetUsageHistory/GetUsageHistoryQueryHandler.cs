using MediatR;
using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.SubscriptionServices.Queries.GetUsageHistory;

public class GetUsageHistoryQueryHandler : IRequestHandler<GetUsageHistoryQuery, AppResponse<IPaginatedList<UsageHistoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUsageHistoryQueryHandler> _logger;

    public GetUsageHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetUsageHistoryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IPaginatedList<UsageHistoryDto>>> Handle(GetUsageHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Build filter predicate
            var filter = BuildFilter(request);

            // Get paginated usage records
            var paginatedUsage = await _unitOfWork.Repository<SubscriptionUsage>()
                .ListAsyncWithPaginated(
                    filter: filter,
                    orderBy: q => q.OrderByDescending(u => u.UsageDate),
                    includeProperties: q => q.Include(u => u.UserSubscription)
                                            .ThenInclude(us => us.Plan),
                    pagination: new PaginationRequest(request.PageNumber, request.PageSize),
                    cancellationToken: cancellationToken);

            // Map to DTOs
            var historyDtos = _mapper.Map<List<UsageHistoryDto>>(paginatedUsage.Items);

            // Create paginated result
            var result = new PaginatedList<UsageHistoryDto>(
                historyDtos,
                paginatedUsage.TotalCount,
                paginatedUsage.PageNumber,
                paginatedUsage.PageSize);

            _logger.LogInformation("Retrieved {Count} usage records for user {UserId}", 
                historyDtos.Count, request.UserId);

            return new AppResponse<IPaginatedList<UsageHistoryDto>>()
                .SetSuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage history for user {UserId}", request.UserId);
            return new AppResponse<IPaginatedList<UsageHistoryDto>>()
                .SetErrorResponse("GetUsageHistory", "An error occurred while retrieving usage history");
        }
    }

    private System.Linq.Expressions.Expression<Func<SubscriptionUsage, bool>> BuildFilter(GetUsageHistoryQuery request)
    {
        return u => u.UserSubscription != null && 
                   u.UserSubscription.UserId == request.UserId &&
                   (string.IsNullOrEmpty(request.FeatureType) || u.FeatureType == request.FeatureType) &&
                   (request.FromDate == null || u.UsageDate >= request.FromDate) &&
                   (request.ToDate == null || u.UsageDate <= request.ToDate);
    }
} 