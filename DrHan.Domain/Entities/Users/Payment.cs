using DrHan.Domain.Constants.Status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Domain.Entities.Users
{
    public class Payment : BaseEntity
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string TransactionId { get; set; } // From payment gateway
        public PaymentStatus PaymentStatus { get; set; } // e.g., "Success", "Failed", "Pending"
        public PaymentMethod PaymentMethod { get; set; } // e.g., "CreditCard", "PayPal"
        public DateTime PaymentDate { get; set; }
        public string? FailureReason { get; set; } // e.g., "Insufficient funds", "Card declined"

        public int UserSubscriptionId { get; set; }
        public virtual UserSubscription? UserSubscription { get; set; }
    }
}
