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
            var jwtSettings = configuration.GetSection("JwtAccessTokenSettings");
            string secretKey = jwtSettings["Secret"]!;
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
                Expires = DateTime.Now.AddMinutes(configuration.GetValue<int>("JwtAccessTokenSettings:ExpirationInMinutes")),
                SigningCredentials = credentials,
                Audience = jwtSettings["Audience"],
                Issuer = jwtSettings["Issuer"]!,
            };

            var handler = new JsonWebTokenHandler();

            string token = handler.CreateToken(tokenDescriptior);

            return token;
        }

        public string CreateIdToken(ApplicationUser user, string role)
        {
            var jwtSettings = configuration.GetSection("JwtIDTokenSettings");
            string secretKey = jwtSettings["Secret"]!;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var audiences = jwtSettings.GetSection("Audience").Get<string[]>() ?? [];

            var claimsList = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Birthdate, user.DateOfBirth.ToString()!),
                new Claim("username", user.UserName!),
                new Claim("role", role),
                new Claim("aud", audiences[0].ToString()),
                new Claim("aud", audiences[1].ToString()),
                new Claim("aud", audiences[2].ToString()),
            };

            
            

            var tokenDescriptior = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimsList),
                Expires = DateTime.Now.AddMinutes(configuration.GetValue<int>("JwtIDTokenSettings:ExpirationInMinutes")),
                SigningCredentials = credentials,
                Issuer = jwtSettings["Issuer"]!,
            };

            var handler = new JsonWebTokenHandler();

            string token = handler.CreateToken(tokenDescriptior);

            return token;
        }

        public string CreateRefreshToken(ApplicationUser user)
        {
            var jwtSettings = configuration.GetSection("JwtRefreshTokenSettings");

            var number = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(number);
            var securityKey = new SymmetricSecurityKey(number);

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
            };

            var handler = new JsonWebTokenHandler();

            string token = handler.CreateToken(tokenDescriptor);

            return token;
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
