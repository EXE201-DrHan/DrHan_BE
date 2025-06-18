using DrHan.Application.DTOs.Payment;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Constants.Status;
using DrHan.Domain.Entities.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace DrHan.Infrastructure.ExternalServices
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOSConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PayOSService> _logger;
        private readonly HttpClient _httpClient;

        public PayOSService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<PayOSService> logger)
        {
            _config = configuration.GetSection("PayOS").Get<PayOSConfiguration>() ?? new PayOSConfiguration();
            _unitOfWork = unitOfWork;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request)
        {
            try
            {
                // Create order code (unique identifier)
                var orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // TODO: Implement actual PayOS API call when SDK is properly configured
                // For now, create a mock payment URL
                var paymentUrl = $"https://pay.payos.vn/web/{orderCode}";

                // Save payment to database
                var payment = new Payment
                {
                    BusinessId = Guid.NewGuid(),
                    Amount = request.Amount,
                    Currency = request.Currency,
                    TransactionId = orderCode.ToString(),
                    PaymentStatus = PaymentStatus.Pending,
                    PaymentMethod = PaymentMethod.PAYOS,
                    PaymentDate = DateTime.UtcNow,
                    UserSubscriptionId = request.UserSubscriptionId,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Payment>().AddAsync(payment);
                await _unitOfWork.CompleteAsync();

                return new PaymentResponseDto
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    TransactionId = payment.TransactionId,
                    PaymentStatus = payment.PaymentStatus,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentDate = payment.PaymentDate,
                    PaymentUrl = paymentUrl,
                    UserSubscriptionId = payment.UserSubscriptionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment");
                throw;
            }
        }

        public async Task<PaymentResponseDto> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                var payment = await _unitOfWork.Repository<Payment>()
                    .FindAsync(p => p.TransactionId == transactionId);

                if (payment == null)
                {
                    throw new InvalidOperationException($"Payment with transaction ID {transactionId} not found");
                }

                // TODO: Check PayOS for latest status when SDK is configured
                // For now, return current status

                return new PaymentResponseDto
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    TransactionId = payment.TransactionId,
                    PaymentStatus = payment.PaymentStatus,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentDate = payment.PaymentDate,
                    FailureReason = payment.FailureReason,
                    UserSubscriptionId = payment.UserSubscriptionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayOS payment status for transaction {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<bool> HandleWebhookAsync(PayOSWebhookDto webhook)
        {
            try
            {
                if (!await ConfirmWebhookDataAsync(webhook))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return false;
                }

                var payment = await _unitOfWork.Repository<Payment>()
                    .FindAsync(p => p.TransactionId == webhook.Data.OrderCode.ToString());

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for order code {OrderCode}", webhook.Data.OrderCode);
                    return false;
                }

                // Update payment status based on webhook data
                if (webhook.Code == "00" && webhook.Desc == "success")
                {
                    payment.PaymentStatus = PaymentStatus.Success;
                    _logger.LogInformation("Payment {TransactionId} completed successfully", payment.TransactionId);
                }
                else
                {
                    payment.PaymentStatus = PaymentStatus.Failed;
                    payment.FailureReason = webhook.Desc;
                    _logger.LogWarning("Payment {TransactionId} failed: {Reason}", payment.TransactionId, webhook.Desc);
                }

                _unitOfWork.Repository<Payment>().Update(payment);
                await _unitOfWork.CompleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayOS webhook");
                return false;
            }
        }

        public async Task<bool> CancelPaymentAsync(string transactionId)
        {
            try
            {
                var payment = await _unitOfWork.Repository<Payment>()
                    .FindAsync(p => p.TransactionId == transactionId);

                if (payment == null)
                {
                    return false;
                }

                if (payment.PaymentStatus != PaymentStatus.Pending)
                {
                    return false;
                }

                // TODO: Cancel payment in PayOS when SDK is configured

                // Update payment status
                payment.PaymentStatus = PaymentStatus.Failed;
                payment.FailureReason = "Payment cancelled by user";

                _unitOfWork.Repository<Payment>().Update(payment);
                await _unitOfWork.CompleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling PayOS payment {TransactionId}", transactionId);
                return false;
            }
        }

        public async Task<bool> ConfirmWebhookDataAsync(PayOSWebhookDto webhook)
        {
            try
            {
                // Verify webhook signature
                var webhookData = System.Text.Json.JsonSerializer.Serialize(webhook.Data);
                var expectedSignature = GenerateSignature(webhookData, _config.ChecksumKey);
                
                return string.Equals(webhook.Signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming webhook data");
                return false;
            }
        }

        private string GenerateSignature(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
} 