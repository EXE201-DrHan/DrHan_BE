using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Users;
using DrHan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace DrHan.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly FirebaseApp _firebaseApp;

    public PushNotificationService(
        ApplicationDbContext context, 
        IConfiguration configuration, 
        ILogger<PushNotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _firebaseApp = InitializeFirebase();
    }

    private FirebaseApp InitializeFirebase()
    {
        var projectId = _configuration["Firebase:ProjectId"];
        var serviceAccountPath = _configuration["Firebase:ServiceAccountPath"];

        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(serviceAccountPath))
        {
            _logger.LogError("Firebase configuration is missing ProjectId or ServiceAccountPath");
            throw new InvalidOperationException("Firebase configuration is incomplete");
        }

        try
        {
            // Check if Firebase app is already initialized
            if (FirebaseApp.DefaultInstance != null)
            {
                return FirebaseApp.DefaultInstance;
            }

            var credential = GoogleCredential.FromFile(serviceAccountPath);
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = credential,
                ProjectId = projectId
            });

            _logger.LogInformation("Firebase initialized successfully for project: {ProjectId}", projectId);
            return app;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase");
            throw;
        }
    }

    public async Task<bool> SendOtpNotificationAsync(int userId, string otpCode, string message)
    {
        try
        {
            var deviceTokens = await GetUserDeviceTokensAsync(userId);
            if (!deviceTokens.Any())
            {
                _logger.LogWarning("No device tokens found for user {UserId}", userId);
                return false;
            }

            var notification = new
            {
                title = "Verification Code",
                body = message,
                data = new { otp_code = otpCode, type = "otp_verification" }
            };

            var successCount = 0;
            foreach (var token in deviceTokens)
            {
                var success = await SendFirebaseNotificationAsync(token, notification);
                if (success) successCount++;
            }

            // Store notification in database
            await StoreNotificationAsync(userId, "OTP_VERIFICATION", "Verification Code", message);

            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendGeneralNotificationAsync(int userId, string title, string message, string? actionUrl = null)
    {
        try
        {
            var deviceTokens = await GetUserDeviceTokensAsync(userId);
            if (!deviceTokens.Any())
            {
                _logger.LogWarning("No device tokens found for user {UserId}", userId);
                return false;
            }

            var notification = new
            {
                title = title,
                body = message,
                data = new { action_url = actionUrl ?? "", type = "general" }
            };

            var successCount = 0;
            foreach (var token in deviceTokens)
            {
                var success = await SendFirebaseNotificationAsync(token, notification);
                if (success) successCount++;
            }

            // Store notification in database
            await StoreNotificationAsync(userId, "GENERAL", title, message, actionUrl);

            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending general notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> RegisterDeviceTokenAsync(int userId, string deviceToken, string platform)
    {
        try
        {
            var existingToken = await _context.UserDeviceTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceToken == deviceToken);

            if (existingToken != null)
            {
                existingToken.Platform = platform;
                existingToken.UpdateAt = DateTime.UtcNow;
                existingToken.IsActive = true;
            }
            else
            {
                var newToken = new UserDeviceToken
                {
                    UserId = userId,
                    DeviceToken = deviceToken,
                    Platform = platform,
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                };
                _context.UserDeviceTokens.Add(newToken);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnregisterDeviceTokenAsync(int userId, string deviceToken)
    {
        try
        {
            var token = await _context.UserDeviceTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceToken == deviceToken);

            if (token != null)
            {
                token.IsActive = false;
                token.UpdateAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device token for user {UserId}", userId);
            return false;
        }
    }

    private async Task<List<string>> GetUserDeviceTokensAsync(int userId)
    {
        return await _context.UserDeviceTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .Select(t => t.DeviceToken)
            .ToListAsync();
    }

    private async Task<bool> SendFirebaseNotificationAsync(string deviceToken, object notificationData)
    {
        try
        {
            var messaging = FirebaseMessaging.GetMessaging(_firebaseApp);
            
            // Extract notification data
            var notificationDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(notificationData));
            
            var title = notificationDict?["title"]?.ToString() ?? "";
            var body = notificationDict?["body"]?.ToString() ?? "";
            var data = notificationDict?.ContainsKey("data") == true 
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(
                    JsonSerializer.Serialize(notificationDict["data"])) 
                : new Dictionary<string, string>();

            var message = new Message()
            {
                Token = deviceToken,
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            var response = await messaging.SendAsync(message);
            _logger.LogInformation("Push notification sent successfully. Response: {Response}", response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending Firebase notification to token {Token}", deviceToken);
            return false;
        }
    }

    private async Task StoreNotificationAsync(int userId, string type, string title, string message, string? actionUrl = null)
    {
        var notification = new DrHan.Domain.Entities.Users.Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            SendViaPush = true,
            IsDelivered = true,
            DeliveredAt = DateTime.UtcNow,
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
} 