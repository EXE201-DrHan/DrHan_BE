namespace DrHan.Application.Interfaces.Services;

public interface IPushNotificationService
{
    Task<bool> SendOtpNotificationAsync(int userId, string otpCode, string message);
    Task<bool> SendGeneralNotificationAsync(int userId, string title, string message, string? actionUrl = null);
    Task<bool> RegisterDeviceTokenAsync(int userId, string deviceToken, string platform);
    Task<bool> UnregisterDeviceTokenAsync(int userId, string deviceToken);
} 