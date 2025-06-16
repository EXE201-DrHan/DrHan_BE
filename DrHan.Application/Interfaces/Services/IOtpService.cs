using DrHan.Domain.Entities.Users;
using DrHan.Domain.Enums;

namespace DrHan.Application.Interfaces.Services;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(int userId, OtpType type, string? phoneNumber = null);
    Task<bool> ValidateOtpAsync(int userId, string code, OtpType type);
    Task<bool> ResendOtpAsync(int userId, OtpType type);
    Task<UserOtp?> GetValidOtpAsync(int userId, OtpType type);
    Task MarkOtpAsUsedAsync(int otpId);
    Task IncrementAttemptsAsync(int otpId);
    Task CleanupExpiredOtpsAsync();
    Task<bool> HasVerifiedOtpAsync(int userId, OtpType type);
} 