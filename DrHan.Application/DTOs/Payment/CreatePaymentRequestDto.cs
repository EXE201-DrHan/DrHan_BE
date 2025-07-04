using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Payment
{
    public class CreatePaymentRequestDto
    {
        [Required]
        public decimal Amount { get; set; }
                
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public int UserSubscriptionId { get; set; }
        
        public string? ReturnUrl { get; set; }
        
        public string? CancelUrl { get; set; }
    }
} 