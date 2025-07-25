using System.Text.Json.Serialization;

namespace DrHan.Application.DTOs.Payment
{
    //public class PayOSWebhookDto
    //{
    //    public string Code { get; set; } = string.Empty;
    //    public string Desc { get; set; } = string.Empty;
    //    public bool Success { get; set; } = false;
    //    public PayOSWebhookDataDto Data { get; set; } = new();
    //    public string Signature { get; set; } = string.Empty;
    //}

    //public class PayOSWebhookDataDto
    //{
    //    public long OrderCode { get; set; }
    //    public decimal Amount { get; set; }
    //    public string Description { get; set; } = string.Empty;
    //    public string AccountNumber { get; set; } = string.Empty;
    //    public string Reference { get; set; } = string.Empty;
    //    public string TransactionDateTime { get; set; } = string.Empty;
    //    public string Currency { get; set; } = string.Empty;
    //    public string PaymentLinkId { get; set; } = string.Empty;
    //    public string Code { get; set; } = string.Empty;
    //    public string Desc { get; set; } = string.Empty;
    //    public string CounterAccountBankId { get; set; } = string.Empty;
    //    public string CounterAccountBankName { get; set; } = string.Empty;
    //    public string CounterAccountName { get; set; } = string.Empty;
    //    public string CounterAccountNumber { get; set; } = string.Empty;
    //    public string VirtualAccountName { get; set; } = string.Empty;
    //    public string VirtualAccountNumber { get; set; } = string.Empty;
    //}
    public class PayOSWebhookDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("desc")]
        public string Desc { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public PayOSWebhookDataDto Data { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public class PayOSWebhookDataDto
    {
        [JsonPropertyName("orderCode")]
        public long OrderCode { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }

        [JsonPropertyName("transactionDateTime")]
        public string TransactionDateTime { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("paymentLinkId")]
        public string PaymentLinkId { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("desc")]
        public string Desc { get; set; }

        [JsonPropertyName("counterAccountBankId")]
        public string CounterAccountBankId { get; set; }

        [JsonPropertyName("counterAccountBankName")]
        public string CounterAccountBankName { get; set; }

        [JsonPropertyName("counterAccountName")]
        public string CounterAccountName { get; set; }

        [JsonPropertyName("counterAccountNumber")]
        public string CounterAccountNumber { get; set; }

        [JsonPropertyName("virtualAccountName")]
        public string VirtualAccountName { get; set; }

        [JsonPropertyName("virtualAccountNumber")]
        public string VirtualAccountNumber { get; set; }
    }

}