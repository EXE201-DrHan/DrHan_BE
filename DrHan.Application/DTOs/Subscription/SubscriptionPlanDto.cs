using System.ComponentModel.DataAnnotations;

namespace DrHan.Application.DTOs.Subscription
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public string BillingCycle { get; set; } = string.Empty;
        public int? UsageQuota { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PlanFeatureDto> Features { get; set; } = new List<PlanFeatureDto>();
    }

    public class PlanFeatureDto
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSubscriptionPlanDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "VND";

        [Required]
        [StringLength(50)]
        public string BillingCycle { get; set; } = string.Empty;

        public int? UsageQuota { get; set; }

        public bool IsActive { get; set; } = true;

        public List<CreatePlanFeatureDto> Features { get; set; } = new List<CreatePlanFeatureDto>();
    }

    public class UpdateSubscriptionPlanDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "VND";

        [Required]
        [StringLength(50)]
        public string BillingCycle { get; set; } = string.Empty;

        public int? UsageQuota { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreatePlanFeatureDto
    {
        [Required]
        [StringLength(100)]
        public string FeatureName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }

    public class UpdatePlanFeatureDto
    {
        [Required]
        [StringLength(100)]
        public string FeatureName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }
    }

    public class AdminUserSubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
} 