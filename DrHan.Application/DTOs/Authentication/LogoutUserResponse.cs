namespace DrHan.Application.DTOs.Authentication
{
    public class LogoutUserResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime LoggedOutAt { get; set; }
    }
} 