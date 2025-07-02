using DrHan.Application.Commons;
using DrHan.Application.DTOs.Payment;
using Net.payOS.Types;

namespace DrHan.Application.Interfaces.Services
{
    public interface IPayOSService
    {
        Task<AppResponse<PaymentResponseDto>> CreatePaymentAsync(CreatePaymentRequestDto request);
        Task<AppResponse<PaymentResponseDto>> GetPaymentStatusAsync(string transactionId);
        Task<AppResponse<CreatePaymentResult>> CreatePaymentTestAsync(CreatePaymentRequestDto request);
        Task<bool> HandleWebhookAsync(PayOSWebhookDto webhook);
        Task<AppResponse<bool>> CancelPaymentAsync(string transactionId);
        Task<bool> ConfirmWebhookDataAsync(PayOSWebhookDto webhook);
    }
} 