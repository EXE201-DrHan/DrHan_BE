using DrHan.Domain.Enums;

namespace DrHan.Domain.Entities.Users;

public class UserOtp : BaseEntity
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public OtpType Type { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public int AttemptsCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 3;
    public bool IsBlocked => AttemptsCount >= MaxAttempts;
    
    public virtual ApplicationUser User { get; set; } = null!;
} 