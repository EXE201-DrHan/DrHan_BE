using DrHan.Application.DTOs.Payment;

namespace DrHan.Application.Interfaces.Services
{
    public interface IPayOSService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request);
        Task<PaymentResponseDto> GetPaymentStatusAsync(string transactionId);
        Task<bool> HandleWebhookAsync(PayOSWebhookDto webhook);
        Task<bool> CancelPaymentAsync(string transactionId);
        Task<bool> ConfirmWebhookDataAsync(PayOSWebhookDto webhook);
    }
} 