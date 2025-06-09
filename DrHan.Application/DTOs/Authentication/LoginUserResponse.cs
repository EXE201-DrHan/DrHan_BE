namespace DrHan.Application.DTOs.Authentication
{
    public class LoginUserResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string? SubscriptionTier { get; set; }
        public string? SubscriptionStatus { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
    }
} 