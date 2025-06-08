using DrHan.Application.Interfaces;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace DrHan.Infrastructure.ExternalServices
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailConfirmationAsync(string email, string subject, string message)
        {
            await SendEmailAsync(email, subject, message);
        }

        public async Task SendPasswordResetAsync(string email, string subject, string message)
        {
            await SendEmailAsync(email, subject, message);
        }

        private async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var mailSettings = _configuration.GetSection("MailSettings");

                var smtpServer = mailSettings["Host"] ?? throw new InvalidOperationException("SMTP Host not configured");

                if (!int.TryParse(mailSettings["Port"], out var smtpPort))
                    throw new InvalidOperationException("Invalid SMTP Port configuration");

                var smtpUsername = mailSettings["Mail"] ?? throw new InvalidOperationException("SMTP Mail not configured");
                var smtpPassword = mailSettings["Password"] ?? throw new InvalidOperationException("SMTP Password not configured");
                var fromEmail = mailSettings["Mail"] ?? throw new InvalidOperationException("From Email not configured");
                var fromName = mailSettings["DisplayName"] ?? "DrHan App";

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);
                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }
    }
}