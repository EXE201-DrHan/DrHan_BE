namespace DrHan.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string email, string subject, string message);
        Task SendPasswordResetAsync(string email, string subject, string message);
    }
} 