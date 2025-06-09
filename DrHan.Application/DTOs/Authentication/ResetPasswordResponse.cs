namespace DrHan.Application.DTOs.Authentication
{
    public class ResetPasswordResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime ResetAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 