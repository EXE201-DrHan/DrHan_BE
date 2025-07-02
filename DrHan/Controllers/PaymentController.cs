using DrHan.Application.Commons;
using DrHan.Application.DTOs.Payment;
using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Constants.Status;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;

namespace DrHan.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPayOSService _payOSService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPayOSService payOSService, ILogger<PaymentController> logger)
        {
            _payOSService = payOSService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new payment with PayOS
        /// </summary>
        /// <param name="request">Payment creation request</param>
        /// <returns>Payment response with payment URL</returns>
        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<AppResponse<PaymentResponseDto>>> CreatePayment([FromBody] CreatePaymentRequestDto request)
        {
            try
            {
                var result = await _payOSService.CreatePaymentAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating payment");
                return BadRequest(new AppResponse<PaymentResponseDto>().SetErrorResponse("error", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return BadRequest(new AppResponse<PaymentResponseDto>().SetErrorResponse("error", "Failed to create payment"));
            }
        }
        [HttpPost("createTest")]
        public async Task<ActionResult<AppResponse<CreatePaymentResult>>> CreatePaymentTest([FromBody] CreatePaymentRequestDto request)
        {
            try
            {
                var result = await _payOSService.CreatePaymentTestAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating payment");
                return BadRequest(new AppResponse<CreatePaymentResult>().SetErrorResponse("error", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return BadRequest(new AppResponse<CreatePaymentResult>().SetErrorResponse("error", "Failed to create payment"));
            }
        }
        /// <summary>
        /// Get payment status by transaction ID
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <returns>Payment status information</returns>
        [HttpGet("status/{transactionId}")]
        [Authorize]
        public async Task<ActionResult<AppResponse<PaymentResponseDto>>> GetPaymentStatus(string transactionId)
        {
            try
            {
                var result = await _payOSService.GetPaymentStatusAsync(transactionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for transaction {TransactionId}", transactionId);
                return BadRequest(new AppResponse<PaymentResponseDto>().SetErrorResponse("error", "Failed to get payment status"));
            }
        }

        /// <summary>
        /// Cancel a pending payment
        /// </summary>
        /// <param name="transactionId">Transaction ID to cancel</param>
        /// <returns>Cancellation result</returns>
        [HttpPost("cancel/{transactionId}")]
        [Authorize]
        public async Task<ActionResult<AppResponse<bool>>> CancelPayment(string transactionId)
        {
            try
            {
                var result = await _payOSService.CancelPaymentAsync(transactionId);
                if (result.IsSucceeded)
                {
                    return Ok(new AppResponse<bool>().SetSuccessResponse(true, "message", "Payment cancelled successfully"));
                }
                return BadRequest(new AppResponse<bool>().SetErrorResponse("error", "Failed to cancel payment"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment {TransactionId}", transactionId);
                return BadRequest(new AppResponse<bool>().SetErrorResponse("error", "Failed to cancel payment"));
            }
        }

        /// <summary>
        /// PayOS webhook endpoint for payment status updates
        /// </summary>
        /// <param name="webhook">Webhook data from PayOS</param>
        /// <returns>Webhook processing result</returns>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<ActionResult> HandleWebhook([FromBody] PayOSWebhookDto webhook)
        {
            try
            {
                var result = await _payOSService.HandleWebhookAsync(webhook);
                if (result)
                {
                    return Ok();
                }
                return BadRequest("Invalid webhook data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayOS webhook");
                return BadRequest("Webhook processing failed");
            }
        }

        /// <summary>
        /// Payment success return endpoint
        /// </summary>
        /// <param name="orderCode">Order code from PayOS</param>
        /// <param name="status">Payment status</param>
        /// <returns>Payment success page or API response</returns>
        [HttpGet("return")]
        [AllowAnonymous]
        public async Task<ActionResult<AppResponse<PaymentResponseDto>>> PaymentReturn([FromQuery] string orderCode, [FromQuery] string status)
        {
            try
            {
                if (string.IsNullOrEmpty(orderCode))
                {
                    return BadRequest(new AppResponse<PaymentResponseDto>().SetErrorResponse("error", "Order code is required"));
                }

                var result = await _payOSService.GetPaymentStatusAsync(orderCode);
                
                if (status?.ToLower() == "paid" || result.IsSucceeded)
                {
                    return Ok(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment return for order {OrderCode}", orderCode);
                return BadRequest(new AppResponse<string>().SetErrorResponse("error","what happended"));
            }
        }

        /// <summary>
        /// Payment cancel return endpoint
        /// </summary>
        /// <param name="orderCode">Order code from PayOS</param>
        /// <returns>Payment cancellation response</returns>
        [HttpGet("cancel")]
        [AllowAnonymous]
        public async Task<ActionResult<AppResponse<PaymentResponseDto>>> PaymentCancel([FromQuery] string orderCode)
        {
            try
            {
                if (string.IsNullOrEmpty(orderCode))
                {
                    return BadRequest(new AppResponse<PaymentResponseDto>().SetErrorResponse("error", "Order code is required"));
                }
                var result = await _payOSService.GetPaymentStatusAsync(orderCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment cancellation for order {OrderCode}", orderCode);
                return BadRequest(new AppResponse<PaymentResponseDto>().SetErrorResponse("error", "Failed to process payment cancellation"));
            }
        }
    }
} 