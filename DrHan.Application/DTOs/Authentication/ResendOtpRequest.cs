namespace DrHan.Application.DTOs.Authentication;

public class ResendOtpRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResendOtpResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int RemainingAttempts { get; set; } = 3;
}

public class ReactivateAccountRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ReactivateAccountResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public bool AccountExists { get; set; }
    public bool IsAlreadyVerified { get; set; }
    public DateTime OtpExpiresAt { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
} 