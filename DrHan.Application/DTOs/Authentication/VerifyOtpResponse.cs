namespace DrHan.Application.DTOs.Authentication;

public class VerifyOtpResponse
{
    public bool IsVerified { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsEmailConfirmed { get; set; }
    public int RemainingAttempts { get; set; }
} 