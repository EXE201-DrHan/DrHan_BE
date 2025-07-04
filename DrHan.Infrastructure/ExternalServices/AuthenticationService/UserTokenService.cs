using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace DrHan.Infrastructure.ExternalServices.AuthenticationService
{
    internal class UserTokenService(
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager
) : IUserTokenService
    {
        public string CreateAccessToken(ApplicationUser user, string role)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            string secretKey = jwtSettings["SecretKey"]!;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptior = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    [
                        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, role),
                    ]),
                Expires = DateTime.Now.AddMinutes(configuration.GetValue<int>("JwtSettings:ExpirationInMinutes")),
                SigningCredentials = credentials,
                Audience = jwtSettings["Audience"],
                Issuer = jwtSettings["Issuer"]!,
            };

            var handler = new JsonWebTokenHandler();

            string token = handler.CreateToken(tokenDescriptior);

            return token;
        }



        public string CreateRefreshToken(ApplicationUser user)
        {
            var jwtSettings = configuration.GetSection("JwtRefreshTokenSettings");

            var secretKey = jwtSettings["SecretKey"]!;
            var key = Encoding.UTF8.GetBytes(secretKey);
            var securityKey = new SymmetricSecurityKey(key);

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>
                    {
                        { JwtRegisteredClaimNames.Sub, user.Id.ToString() }
                    },
                Expires = DateTime.Now.AddMinutes(configuration.GetValue<int>("JwtRefreshTokenSettings:ExpirationInMinutes")),
                SigningCredentials = credentials,
                Issuer = jwtSettings["Issuer"]!,
                Audience = jwtSettings["Audience"]!,
            };

            var handler = new JsonWebTokenHandler();

            string token = handler.CreateToken(tokenDescriptor);

            return token;
        }

        public DateTime GetAccessTokenExpiration()
        {
            // Get expiration time from configuration
            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationMinutes = jwtSettings.GetValue<int>("ExpirationInMinutes");

            // Return UTC time when the access token will expire
            return DateTime.Now.AddMinutes(expirationMinutes);
        }

        public async Task<bool> ValidateRefreshToken(string userId, string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(refreshToken))
                {
                    return false;
                }

                if (IsJwtToken(refreshToken))
                {
                    return await ValidateJwtRefreshToken(userId, refreshToken);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                // TODO: Add proper logging service injection
                Console.WriteLine($"Error validating refresh token: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ValidateJwtRefreshToken(string userId, string refreshToken)
        {
            try
            {
                var jwtSettings = configuration.GetSection("JwtRefreshTokenSettings");
                var secretKey = jwtSettings["SecretKey"];

                if (string.IsNullOrEmpty(secretKey))
                {
                    return false;
                }

                var key = Encoding.UTF8.GetBytes(secretKey);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true
                };

                var handler = new JsonWebTokenHandler();
                var result = await handler.ValidateTokenAsync(refreshToken, validationParameters);

                if (!result.IsValid)
                {
                    return false;
                }

                // Verify the token belongs to the correct user
                var tokenUserId = result.Claims?.FirstOrDefault(x => x.Key == JwtRegisteredClaimNames.Sub).Value?.ToString();
                if (tokenUserId != userId)
                {
                    return false;
                }

                // Additional check: verify token type if you added that claim
                var tokenType = result.Claims?.FirstOrDefault(x => x.Key == "token_type").Value?.ToString();
                if (!string.IsNullOrEmpty(tokenType) && tokenType != "refresh")
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                // TODO: Add proper logging service injection
                Console.WriteLine($"Error validating JWT refresh token: {ex.Message}");
                return false;
            }
        }
        private bool IsJwtToken(string token)
        {
            return token.Split('.').Length == 3;
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }

        public async Task RevokeUserToken(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found");
            }

            user.RefreshToken = null;
            await userManager.UpdateAsync(user);
        }

        
    }

}
