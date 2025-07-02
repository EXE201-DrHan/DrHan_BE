namespace DrHan.Application.DTOs.Payment
{
    public class PayOSConfiguration
    {
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    // PayOS API request/response models
    public class PayOSCreatePaymentRequest
    {
        public int orderCode { get; set; }
        public int amount { get; set; }
        public string description { get; set; } = string.Empty;
        public string returnUrl { get; set; } = string.Empty;
        public string cancelUrl { get; set; } = string.Empty;
        public string signature { get; set; } = string.Empty;
    }
    public class PayOSCreatePaymentRequestTest
    {
        public long orderCode { get; set; }
        public int amount { get; set; }
        public string description { get; set; } = string.Empty;
        public string returnUrl { get; set; } = string.Empty;
        public string cancelUrl { get; set; } = string.Empty;
        public string signature { get; set; } = string.Empty;
    }
    public class PayOSCreatePaymentResponse
    {
        public string error { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public PayOSPaymentData data { get; set; } = new();
    }

    public class PayOSPaymentData
    {
        public string bin { get; set; } = string.Empty;
        public string accountNumber { get; set; } = string.Empty;
        public string accountName { get; set; } = string.Empty;
        public int amount { get; set; }
        public string description { get; set; } = string.Empty;
        public int orderCode { get; set; }
        public string currency { get; set; } = string.Empty;
        public string paymentLinkId { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string checkoutUrl { get; set; } = string.Empty;
        public string qrCode { get; set; } = string.Empty;
    }

    public class PayOSPaymentStatusResponse
    {
        public string error { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public PayOSPaymentStatusData data { get; set; } = new();
    }

    public class PayOSPaymentStatusData
    {
        public int orderCode { get; set; }
        public int amount { get; set; }
        public int amountPaid { get; set; }
        public int amountRemaining { get; set; }
        public string status { get; set; } = string.Empty;
        public string createdAt { get; set; } = string.Empty;
        public List<PayOSTransaction> transactions { get; set; } = new();
    }

    public class PayOSTransaction
    {
        public string reference { get; set; } = string.Empty;
        public int amount { get; set; }
        public string accountNumber { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string transactionDateTime { get; set; } = string.Empty;
        public string virtualAccountName { get; set; } = string.Empty;
        public string virtualAccountNumber { get; set; } = string.Empty;
        public string counterAccountBankId { get; set; } = string.Empty;
        public string counterAccountBankName { get; set; } = string.Empty;
        public string counterAccountName { get; set; } = string.Empty;
        public string counterAccountNumber { get; set; } = string.Empty;
    }
} 