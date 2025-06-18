using DrHan.Domain.Constants.Status;

namespace DrHan.Application.DTOs.Payment
{
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? FailureReason { get; set; }
        public string? PaymentUrl { get; set; }
        public int UserSubscriptionId { get; set; }
    }
} 