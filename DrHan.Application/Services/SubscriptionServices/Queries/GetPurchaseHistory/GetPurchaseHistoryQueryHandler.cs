using MediatR;
using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;
using DrHan.Application.Interfaces.Repository;
using DrHan.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DrHan.Application.Services.SubscriptionServices.Queries.GetPurchaseHistory;

public class GetPurchaseHistoryQueryHandler : IRequestHandler<GetPurchaseHistoryQuery, AppResponse<IPaginatedList<PurchaseHistoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPurchaseHistoryQueryHandler> _logger;

    public GetPurchaseHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetPurchaseHistoryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppResponse<IPaginatedList<PurchaseHistoryDto>>> Handle(GetPurchaseHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Build filter predicate
            var filter = BuildFilter(request);

            // Get paginated payments
            var paginatedPayments = await _unitOfWork.Repository<Payment>()
                .ListAsyncWithPaginated(
                    filter: filter,
                    orderBy: q => q.OrderByDescending(p => p.PaymentDate),
                    includeProperties: q => q.Include(p => p.UserSubscription)
                                            .ThenInclude(us => us.Plan),
                    pagination: new PaginationRequest(request.PageNumber, request.PageSize),
                    cancellationToken: cancellationToken);

            // Map to DTOs
            var historyDtos = _mapper.Map<List<PurchaseHistoryDto>>(paginatedPayments.Items);

            // Create paginated result
            var result = new PaginatedList<PurchaseHistoryDto>(
                historyDtos,
                paginatedPayments.TotalCount,
                paginatedPayments.PageNumber,
                paginatedPayments.PageSize);

            _logger.LogInformation("Retrieved {Count} purchase records for user {UserId}", 
                historyDtos.Count, request.UserId);

            return new AppResponse<IPaginatedList<PurchaseHistoryDto>>()
                .SetSuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase history for user {UserId}", request.UserId);
            return new AppResponse<IPaginatedList<PurchaseHistoryDto>>()
                .SetErrorResponse("GetPurchaseHistory", "An error occurred while retrieving purchase history");
        }
    }

    private System.Linq.Expressions.Expression<Func<Payment, bool>> BuildFilter(GetPurchaseHistoryQuery request)
    {
        return p => p.UserSubscription != null && 
                   p.UserSubscription.UserId == request.UserId &&
                   (request.FromDate == null || p.PaymentDate >= request.FromDate) &&
                   (request.ToDate == null || p.PaymentDate <= request.ToDate);
    }
} 