using DrHan.Application.Interfaces;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Reflection;

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

        public async Task SendOtpAsync(string email, string fullName, string otpCode)
        {
            var subject = "Email Verification - DrHan";
            var htmlTemplate = await LoadOtpTemplateAsync();
            
            // Replace placeholders with actual values
            var htmlBody = htmlTemplate
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{OTP_CODE}}", otpCode);

            await SendEmailAsync(email, subject, htmlBody);
        }

        private async Task<string> LoadOtpTemplateAsync()
        {
            try
            {
                // Get the path to the template file
                var currentDirectory = Directory.GetCurrentDirectory();
                var templatePath = Path.Combine(currentDirectory, "DrHan.Infrastructure", "EmailTemplates", "OtpTemplate.html");
                
                // If not found in current directory, try relative to assembly location
                if (!File.Exists(templatePath))
                {
                    var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                    templatePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "DrHan.Infrastructure", "EmailTemplates", "OtpTemplate.html");
                }

                if (File.Exists(templatePath))
                {
                    return await File.ReadAllTextAsync(templatePath);
                }
                
                // Fallback template if file not found
                return GetFallbackOtpTemplate();
            }
            catch (Exception)
            {
                // Return fallback template in case of any error
                return GetFallbackOtpTemplate();
            }
        }

        private static string GetFallbackOtpTemplate()
        {
            return @"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <div style='background-color: #667eea; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                    <h1 style='margin: 0;'>DrHan</h1>
                    <p style='margin: 10px 0 0 0;'>Email Verification</p>
                </div>
                <div style='background-color: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-radius: 0 0 10px 10px;'>
                    <h2 style='color: #333;'>Hello {{FULL_NAME}},</h2>
                    <p style='color: #666; line-height: 1.6;'>Welcome to DrHan! Please use the following verification code to complete your registration:</p>
                    <div style='background-color: #f8f9fa; border: 2px solid #667eea; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;'>
                        <p style='margin: 0; color: #666; font-size: 14px;'>VERIFICATION CODE</p>
                        <h1 style='margin: 10px 0; color: #667eea; font-size: 32px; letter-spacing: 4px; font-family: monospace;'>{{OTP_CODE}}</h1>
                        <p style='margin: 0; color: #dc3545; font-size: 14px;'>⏰ Expires in 10 minutes</p>
                    </div>
                    <p style='color: #666; line-height: 1.6;'>If you didn't create an account with DrHan, please ignore this email.</p>
                    <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 30px 0;'>
                    <p style='color: #999; font-size: 14px; text-align: center;'>Thank you for choosing DrHan</p>
                </div>
            </body>
            </html>";
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