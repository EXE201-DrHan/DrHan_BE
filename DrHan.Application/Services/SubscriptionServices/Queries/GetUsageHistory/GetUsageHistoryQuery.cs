using MediatR;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Subscription;

namespace DrHan.Application.Services.SubscriptionServices.Queries.GetUsageHistory;

public class GetUsageHistoryQuery : IRequest<AppResponse<IPaginatedList<UsageHistoryDto>>>
{
    public int UserId { get; set; }
    public string? FeatureType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
} 