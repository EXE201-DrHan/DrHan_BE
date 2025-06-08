namespace DrHan.Application.DTOs.Authentication
{
    public class RevokeUserResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RevokedAt { get; set; }
        public string? Reason { get; set; }
    }
} 