using MediatR;
using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.SubscriptionServices.Queries.GetSubscriptionHistory;

public class GetSubscriptionHistoryQueryHandler : IRequestHandler<GetSubscriptionHistoryQuery, AppResponse<IPaginatedList<SubscriptionHistoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSubscriptionHistoryQueryHandler> _logger;

    public GetSubscriptionHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetSubscriptionHistoryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IPaginatedList<SubscriptionHistoryDto>>> Handle(GetSubscriptionHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Build filter predicate
            var filter = BuildFilter(request);

            // Get paginated subscriptions
            var paginatedSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .ListAsyncWithPaginated(
                    filter: filter,
                    orderBy: q => q.OrderByDescending(s => s.CreatedAt),
                    includeProperties: q => q.Include(s => s.Plan),
                    pagination: new PaginationRequest(request.PageNumber, request.PageSize),
                    cancellationToken: cancellationToken);

            // Map to DTOs
            var historyDtos = _mapper.Map<List<SubscriptionHistoryDto>>(paginatedSubscriptions.Items);

            // Create paginated result
            var result = new PaginatedList<SubscriptionHistoryDto>(
                historyDtos,
                paginatedSubscriptions.TotalCount,
                paginatedSubscriptions.PageNumber,
                paginatedSubscriptions.PageSize);

            _logger.LogInformation("Retrieved {Count} subscription records for user {UserId}", 
                historyDtos.Count, request.UserId);

            return new AppResponse<IPaginatedList<SubscriptionHistoryDto>>()
                .SetSuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription history for user {UserId}", request.UserId);
            return new AppResponse<IPaginatedList<SubscriptionHistoryDto>>()
                .SetErrorResponse("GetSubscriptionHistory", "An error occurred while retrieving subscription history");
        }
    }

    private System.Linq.Expressions.Expression<Func<UserSubscription, bool>> BuildFilter(GetSubscriptionHistoryQuery request)
    {
        return s => s.UserId == request.UserId &&
                   (request.FromDate == null || s.CreatedAt >= request.FromDate) &&
                   (request.ToDate == null || s.CreatedAt <= request.ToDate);
    }
} 