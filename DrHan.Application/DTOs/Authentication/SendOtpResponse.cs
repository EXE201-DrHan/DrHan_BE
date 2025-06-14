namespace DrHan.Application.DTOs.Authentication;

public class SendOtpResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int RemainingAttempts { get; set; }
} 