namespace DrHan.Application.DTOs.Authentication
{
    public class SendPasswordResetResponse
    {
        public string Email { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
} 