using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Payment;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Constants.Status;
using DrHan.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using Net.payOS.Types;
using System.Security.Cryptography.Xml;
using Net.payOS;
using Azure.Core;

namespace DrHan.Infrastructure.ExternalServices
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOSConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PayOSService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ISubscriptionService _subscriptionService;
        private readonly PayOS _payOs;
        public PayOSService(
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            ILogger<PayOSService> logger,
            IHttpClientFactory httpClientFactory,
            ISubscriptionService subscriptionService,
            PayOS payOs)
        {
            _config = configuration.GetSection("PayOS").Get<PayOSConfiguration>() ?? new PayOSConfiguration();
            _unitOfWork = unitOfWork;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("PayOS");
            _subscriptionService = subscriptionService;

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("x-client-id", _config.ClientId);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
            _payOs = payOs;
        }

        public async Task<AppResponse<PaymentResponseDto>> CreatePaymentAsync(CreatePaymentRequestDto request)
        {
            try
            {
                // Validate that the UserSubscription exists
                var (isValid, errorMessage) = await ValidateUserSubscriptionAsync(request.UserSubscriptionId);

                if (!isValid)
                {
                    return new AppResponse<PaymentResponseDto>().SetErrorResponse("ValidationError", errorMessage);
                }

                var orderCode = (long)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Create payment items (required by PayOS SDK)
                ItemData item = new ItemData($"Subscription Payment {request.UserSubscriptionId}", 1, (int)request.Amount);
                List<ItemData> items = new List<ItemData> { item };

                // Generate signature for PayOS request
                var description = $"Sub {request.UserSubscriptionId}";
                var returnUrl = request.ReturnUrl ?? _config.ReturnUrl;
                var cancelUrl = request.CancelUrl ?? _config.CancelUrl;
                
                var dataToSign = $"amount={request.Amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
                var signature = GenerateSignature(dataToSign, _config.ChecksumKey);

                // Create PaymentData object for PayOS SDK
                PaymentData paymentData = new PaymentData(
                    orderCode, 
                    (int)request.Amount, 
                    description, 
                    items, 
                    returnUrl, 
                    cancelUrl, 
                    signature);

                // Call PayOS SDK
                var createPaymentResult = await _payOs.createPaymentLink(paymentData);

                if (createPaymentResult == null)
                {
                    _logger.LogError("PayOS returned null result");
                    return new AppResponse<PaymentResponseDto>().SetErrorResponse("PayOSError", "Payment creation failed: null result from PayOS");
                }

                // Create payment record in database
                var payment = new Payment
                {
                    BusinessId = Guid.NewGuid(),
                    Amount = request.Amount,
                    Currency = "VND",
                    TransactionId = orderCode.ToString(),
                    PaymentStatus = PaymentStatus.Pending,
                    PaymentMethod = PaymentMethod.PAYOS,
                    PaymentDate = DateTime.Now,
                    UserSubscriptionId = request.UserSubscriptionId,
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now
                };

                await _unitOfWork.Repository<Payment>().AddAsync(payment);
                await _unitOfWork.CompleteAsync();

                var result = new PaymentResponseDto
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    TransactionId = payment.TransactionId,
                    PaymentStatus = payment.PaymentStatus,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentDate = payment.PaymentDate,
                    PaymentUrl = createPaymentResult.checkoutUrl,
                    UserSubscriptionId = payment.UserSubscriptionId
                };

                return new AppResponse<PaymentResponseDto>().SetSuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment for UserSubscriptionId {UserSubscriptionId}", request.UserSubscriptionId);
                return new AppResponse<PaymentResponseDto>().SetErrorResponse("Exception", "An error occurred while creating the payment");
            }
        }

        public async Task<AppResponse<PaymentResponseDto>> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                var payment = await _unitOfWork.Repository<Payment>()
                    .FindAsync(p => p.TransactionId == transactionId);

                if (payment == null)
                {
                    return new AppResponse<PaymentResponseDto>().SetErrorResponse("NotFound", $"Payment with transaction ID {transactionId} not found");
                }

                // Check PayOS for latest status
                //var response = await _httpClient.GetAsync($"/v2/payment-requests/{transactionId}");

                //if (response.IsSuccessStatusCode)
                //{
                //    var payOSResponse = await response.Content.ReadFromJsonAsync<PayOSPaymentStatusResponse>();

                //    if (payOSResponse != null && string.IsNullOrEmpty(payOSResponse.error))
                //    {
                //        // Update payment status based on PayOS response
                //        var newStatus = MapPayOSStatusToPaymentStatus(payOSResponse.data.status);

                //        if (newStatus != payment.PaymentStatus)
                //        {
                //            payment.PaymentStatus = newStatus;
                //            payment.UpdateAt = DateTime.Now;

                //            _unitOfWork.Repository<Payment>().Update(payment);
                //            await _unitOfWork.CompleteAsync();

                //            // Handle status changes
                //            if (newStatus == PaymentStatus.Success)
                //            {
                //                await ActivateSubscriptionAsync(payment);
                //            }
                //            else if (newStatus == PaymentStatus.Failed)
                //            {
                //                await HandleFailedPaymentAsync(payment);
                //            }
                //        }
                //    }
                //}

                var result = new PaymentResponseDto
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

                return new AppResponse<PaymentResponseDto>().SetSuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayOS payment status for transaction {TransactionId}", transactionId);
                return new AppResponse<PaymentResponseDto>().SetErrorResponse("Exception", "An error occurred while checking payment status");
            }
        }

        public async Task<bool> HandleWebhookAsync(PayOSWebhookDto webhook)
        {
            try
            {
                //if (!await ConfirmWebhookDataAsync(webhook))
                //{
                //    _logger.LogWarning("Invalid webhook signature");
                //    return false;
                //}


                var payments = await _unitOfWork.Repository<Payment>()
                    .ListAsync(
                        filter: p => p.TransactionId == webhook.Data.OrderCode.ToString(),
                        includeProperties: query => query
                            .Include(p => p.UserSubscription)
                                .ThenInclude(us => us.Plan)
                    );
                var payment = payments.FirstOrDefault();

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for order code {OrderCode}", webhook.Data.OrderCode);
                    return false;
                }

                // Update payment status based on webhook
                var oldStatus = payment.PaymentStatus;

                if (webhook.Success)
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

                payment.UpdateAt = DateTime.Now;
                _unitOfWork.Repository<Payment>().Update(payment);

                // Handle status changes
                if (oldStatus != payment.PaymentStatus)
                {
                    if (payment.PaymentStatus == PaymentStatus.Success)
                    {
                        await ActivateSubscriptionAsync(payment);
                    }
                    else if (payment.PaymentStatus == PaymentStatus.Failed)
                    {
                        await HandleFailedPaymentAsync(payment);
                    }
                }

                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayOS webhook");
                return false;
            }
        }

        public async Task<AppResponse<bool>> CancelPaymentAsync(string transactionId)
        {
            try
            {
                var payment = await _unitOfWork.Repository<Payment>()
                    .FindAsync(p => p.TransactionId == transactionId);

                if (payment == null)
                {
                    return new AppResponse<bool>().SetErrorResponse("NotFound", $"Payment with transaction ID {transactionId} not found");
                }

                if (payment.PaymentStatus != PaymentStatus.Pending)
                {
                    return new AppResponse<bool>().SetErrorResponse("InvalidStatus", "Only pending payments can be cancelled");
                }

                // Cancel payment in PayOS
                var response = await _httpClient.PostAsync($"/v2/payment-requests/{transactionId}/cancel", null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("PayOS cancel API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return new AppResponse<bool>().SetErrorResponse("PayOSError", "Failed to cancel payment with PayOS");
                }

                // Update payment status
                payment.PaymentStatus = PaymentStatus.Failed;
                payment.FailureReason = "Payment cancelled by user";
                payment.UpdateAt = DateTime.Now;

                _unitOfWork.Repository<Payment>().Update(payment);
                await _unitOfWork.CompleteAsync();

                await HandleFailedPaymentAsync(payment);

                return new AppResponse<bool>().SetSuccessResponse(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling PayOS payment {TransactionId}", transactionId);
                return new AppResponse<bool>().SetErrorResponse("Exception", "An error occurred while cancelling the payment");
            }
        }

        public async Task<bool> ConfirmWebhookDataAsync(PayOSWebhookDto webhook)
        {
            try
            {
                var webhookData = JsonSerializer.Serialize(webhook.Data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var expectedSignature = GenerateSignature(webhookData, _config.ChecksumKey);

                //return string.Equals(webhook.Signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming webhook data");
                return false;
            }
        }

        private PaymentStatus MapPayOSStatusToPaymentStatus(string payOSStatus)
        {
            return payOSStatus?.ToLower() switch
            {
                "paid" => PaymentStatus.Success,
                "cancelled" => PaymentStatus.Failed,
                "expired" => PaymentStatus.Failed,
                "pending" => PaymentStatus.Pending,
                _ => PaymentStatus.Pending
            };
        }

        private async Task ActivateSubscriptionAsync(Payment payment)
        {
            try
            {
                if (payment.UserSubscription == null)
                {
                    // Load the subscription if not included
                    var subscriptionSub = await _unitOfWork.Repository<UserSubscription>()
                        .FindAsync(us => us.Id == payment.UserSubscriptionId,
                                 query => query.Include(us => us.Plan));

                    if (subscriptionSub == null)
                    {
                        _logger.LogWarning("No subscription found for payment {PaymentId}", payment.Id);
                        return;
                    }
                    payment.UserSubscription = subscriptionSub;
                }

                var subscription = payment.UserSubscription;

                subscription.Status = UserSubscriptionStatus.Active;
                subscription.StartDate = DateTime.Now;

                // Set end date based on billing cycle
                if (subscription.Plan != null)
                {
                    subscription.EndDate = subscription.Plan.BillingCycle?.ToLower() switch
                    {
                        "monthly" => DateTime.Now.AddMonths(1),
                        "yearly" => DateTime.Now.AddYears(1),
                        "quarterly" => DateTime.Now.AddMonths(3),
                        "weekly" => DateTime.Now.AddDays(7),
                        _ => DateTime.Now.AddMonths(1) // Default to monthly
                    };
                }
                else
                {
                    subscription.EndDate = DateTime.Now.AddMonths(1); // Default to monthly
                }

                _unitOfWork.Repository<UserSubscription>().Update(subscription);

                await _subscriptionService.RefreshPlanCache();

                _logger.LogInformation("Subscription {SubscriptionId} activated for user {UserId}",
                    subscription.Id, subscription.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating subscription for payment {PaymentId}", payment.Id);
            }
        }

        private async Task HandleFailedPaymentAsync(Payment payment)
        {
            try
            {
                if (payment.UserSubscription == null)
                {
                    // Load the subscription if not included
                    var subscriptionSub = await _unitOfWork.Repository<UserSubscription>()
                        .FindAsync(us => us.Id == payment.UserSubscriptionId);

                    if (subscriptionSub == null)
                    {
                        return; // No subscription to handle
                    }
                    payment.UserSubscription = subscriptionSub;
                }

                var subscription = payment.UserSubscription;

                // Only deactivate if subscription was pending (new subscription)
                if (subscription.Status == UserSubscriptionStatus.Pending)
                {
                    subscription.Status = UserSubscriptionStatus.Inactive;
                    _unitOfWork.Repository<UserSubscription>().Update(subscription);

                    _logger.LogInformation("Subscription {SubscriptionId} marked as inactive due to failed payment {PaymentId}",
                        subscription.Id, payment.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling failed payment for subscription {SubscriptionId}",
                    payment.UserSubscription?.Id);
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

        private async Task<(bool isValid, string errorMessage)> ValidateUserSubscriptionAsync(int userSubscriptionId)
        {
            try
            {
                var userSubscription = await _unitOfWork.Repository<UserSubscription>()
                    .FindAsync(us => us.Id == userSubscriptionId);

                if (userSubscription == null)
                {
                    // Check if there are any UserSubscriptions at all
                    var anySubscriptions = await _unitOfWork.Repository<UserSubscription>()
                        .CountAsync();

                    if (anySubscriptions == 0)
                    {
                        return (false, $"UserSubscription with ID {userSubscriptionId} not found. No subscriptions exist in the system.");
                    }

                    // Get the latest subscription IDs for reference
                    var latestSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                        .ListAsync(orderBy: q => q.OrderByDescending(us => us.Id));

                    var latestIds = string.Join(", ", latestSubscriptions.Take(5).Select(us => us.Id));

                    return (false, $"UserSubscription with ID {userSubscriptionId} not found. Latest subscription IDs: {latestIds}");
                }

                return (true, "UserSubscription is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating UserSubscription {UserSubscriptionId}", userSubscriptionId);
                return (false, $"Error validating UserSubscription: {ex.Message}");
            }
        }

        public async Task<AppResponse<CreatePaymentResult>> CreatePaymentTestAsync(CreatePaymentRequestDto request)
        {
            try
            {
                var orderCode = (long)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Create PayOS payment request
                var payOSRequest = new PayOSCreatePaymentRequestTest
                {
                    orderCode = orderCode,
                    amount = (int)request.Amount,
                    description = $"{request.UserSubscriptionId}",
                    returnUrl = request.ReturnUrl ?? _config.ReturnUrl,
                    cancelUrl = request.CancelUrl ?? _config.CancelUrl
                };
                // Generate signature for PayOS request
                var dataToSign = $"amount={payOSRequest.amount}&cancelUrl={payOSRequest.cancelUrl}&description={payOSRequest.description}&orderCode={payOSRequest.orderCode}&returnUrl={payOSRequest.returnUrl}";
                payOSRequest.signature = GenerateSignature(dataToSign, _config.ChecksumKey);
                ItemData item = new ItemData("Teehee",1,3000);
                List<ItemData> items = new List<ItemData> { item };

                
                PaymentData dataTest = new PaymentData(orderCode, payOSRequest.amount,payOSRequest.description, items, payOSRequest.returnUrl
                    , payOSRequest.cancelUrl, payOSRequest.signature);
                var CreatePaymentResult = await _payOs.createPaymentLink(dataTest);
                // Call PayOS API
                //if (CreatePaymentResult.status != "00")
                //{
                //    return new AppResponse<CreatePaymentResult>().SetErrorResponse("PayOSError", $"PayOS API error: {CreatePaymentResult.status}");
                //}

                return new AppResponse<CreatePaymentResult>().SetSuccessResponse(CreatePaymentResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment for UserSubscriptionId {UserSubscriptionId}", request.UserSubscriptionId);
                return new AppResponse<CreatePaymentResult>().SetErrorResponse("Exception", "An error occurred while creating the payment");
            }

        }
    }
}