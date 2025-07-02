using DrHan.Domain.Constants.Status;

namespace DrHan.Application.DTOs.Subscription;

public class PurchaseHistoryDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? FailureReason { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
}

public class UsageHistoryDto
{
    public int Id { get; set; }
    public string FeatureType { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public DateTime UsageDate { get; set; }
    public string? ResourceUsed { get; set; }
    public string PlanName { get; set; } = string.Empty;
}

public class SubscriptionHistoryDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal PlanPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public UserSubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DaysActive { get; set; }
}

public class UserHistoryDto
{
    public List<PurchaseHistoryDto> PurchaseHistory { get; set; } = new();
    public List<UsageHistoryDto> UsageHistory { get; set; } = new();
    public List<SubscriptionHistoryDto> SubscriptionHistory { get; set; } = new();
}

public class HistoryFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? HistoryType { get; set; } // "purchase", "usage", "subscription", "all"
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
} 