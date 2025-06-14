namespace DrHan.Domain.Entities.Users;

public class UserDeviceToken : BaseEntity
{
    public int UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // iOS, Android, Web
    public bool IsActive { get; set; } = true;
    
    public virtual ApplicationUser User { get; set; } = null!;
} 