using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Enums;
using DrHan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DrHan.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly IPushNotificationService _pushNotificationService;

    public OtpService(ApplicationDbContext context, IPushNotificationService pushNotificationService)
    {
        _context = context;
        _pushNotificationService = pushNotificationService;
    }

    public async Task<string> GenerateOtpAsync(int userId, OtpType type, string? phoneNumber = null)
    {
        // Invalidate any existing OTPs for this user and type
        var existingOtps = await _context.UserOtps
            .Where(o => o.UserId == userId && o.Type == type && !o.IsUsed)
            .ToListAsync();

        foreach (var otp in existingOtps)
        {
            otp.IsUsed = true;
            otp.UpdateAt = DateTime.UtcNow;
        }

        // Generate new 6-digit OTP
        var otpCode = GenerateRandomOtp();
        
        var newOtp = new UserOtp
        {
            UserId = userId,
            Code = otpCode,
            Type = type,
            PhoneNumber = phoneNumber ?? string.Empty,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5), // 5 minutes expiry
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        _context.UserOtps.Add(newOtp);
        await _context.SaveChangesAsync();

        // Send push notification
        var message = type switch
        {
            OtpType.EmailVerification => $"Your email verification code is: {otpCode}",
            OtpType.PhoneVerification => $"Your phone verification code is: {otpCode}",
            OtpType.PasswordReset => $"Your password reset code is: {otpCode}",
            OtpType.TwoFactorAuthentication => $"Your 2FA code is: {otpCode}",
            _ => $"Your verification code is: {otpCode}"
        };

        await _pushNotificationService.SendOtpNotificationAsync(userId, otpCode, message);

        return otpCode;
    }

    public async Task<bool> ValidateOtpAsync(int userId, string code, OtpType type)
    {
        var otp = await GetValidOtpAsync(userId, type);
        
        if (otp == null || otp.Code != code || otp.IsExpired || otp.IsBlocked)
        {
            if (otp != null && !otp.IsBlocked)
            {
                await IncrementAttemptsAsync(otp.Id);
            }
            return false;
        }

        await MarkOtpAsUsedAsync(otp.Id);
        return true;
    }

    public async Task<bool> ResendOtpAsync(int userId, OtpType type)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        await GenerateOtpAsync(userId, type);
        return true;
    }

    public async Task<UserOtp?> GetValidOtpAsync(int userId, OtpType type)
    {
        return await _context.UserOtps
            .Where(o => o.UserId == userId && 
                       o.Type == type && 
                       !o.IsUsed && 
                       !o.IsBlocked &&
                       o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreateAt)
            .FirstOrDefaultAsync();
    }

    public async Task MarkOtpAsUsedAsync(int otpId)
    {
        var otp = await _context.UserOtps.FindAsync(otpId);
        if (otp != null)
        {
            otp.IsUsed = true;
            otp.UpdateAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementAttemptsAsync(int otpId)
    {
        var otp = await _context.UserOtps.FindAsync(otpId);
        if (otp != null)
        {
            otp.AttemptsCount++;
            otp.UpdateAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CleanupExpiredOtpsAsync()
    {
        var expiredOtps = await _context.UserOtps
            .Where(o => o.ExpiresAt < DateTime.UtcNow || o.IsUsed)
            .ToListAsync();

        _context.UserOtps.RemoveRange(expiredOtps);
        await _context.SaveChangesAsync();
    }

    private static string GenerateRandomOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var random = BitConverter.ToUInt32(bytes, 0);
        return (random % 1000000).ToString("D6");
    }
} 