namespace DrHan.Application.DTOs.Authentication
{
    public class RefreshTokenResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime TokenExpiresAt { get; set; }
    }
} 