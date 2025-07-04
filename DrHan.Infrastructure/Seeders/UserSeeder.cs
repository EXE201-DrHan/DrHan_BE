using DrHan.Domain.Constants;
using DrHan.Domain.Constants.Roles;
using DrHan.Domain.Constants.Status;
using DrHan.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace DrHan.Infrastructure.Seeders
{
    public static class UserSeeder
    {
        public static async Task SeedRolesAndUsersAsync(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            // Define roles
            string[] roles = { UserRoles.Staff, UserRoles.Admin, UserRoles.Customer, UserRoles.Nutritionist };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole(role));
                }
            }

            // Users to seed with detailed information
            var users = new List<(string FullName, string Email, string UserName, string Role, DateTime? DateOfBirth, Gender Gender, string SubscriptionTier, string SubscriptionStatus, DateTime? SubscriptionExpiresAt, string PhoneNumber)>
            {
                // Customers
                ("Sarah Johnson", "customer1@example.com", "SarahCustomer", UserRoles.Customer, new DateTime(1985, 3, 15), Gender.Female, "Premium", "Active", DateTime.Now.AddMonths(12), "+1234567890"),
                ("Michael Chen", "customer2@example.com", "MichaelCustomer", UserRoles.Customer, new DateTime(1990, 7, 22), Gender.Male, "Basic", "Active", DateTime.Now.AddMonths(1), "+1987654321"),
                ("Emma Davis", "customer3@example.com", "EmmaCustomer", UserRoles.Customer, new DateTime(1992, 11, 8), Gender.Female, "Free", "Active", null, "+1555123456"),
                ("James Wilson", "customer4@example.com", "JamesCustomer", UserRoles.Customer, new DateTime(1988, 5, 3), Gender.Male, "Premium", "Expired", DateTime.Now.AddDays(-30), "+1122334455"),
                
                // Staff
                ("Lisa Martinez", "staff1@example.com", "StaffLisa", UserRoles.Staff, new DateTime(1987, 12, 25), Gender.Female, null, null, null, "+1666777888"),
                ("Robert Thompson", "staff2@example.com", "StaffRobert", UserRoles.Staff, new DateTime(1983, 4, 18), Gender.Male, null, null, null, "+1555666777"),
                
                // Nutritionists
                ("Dr. Alice Wong", "nutritionist1@example.com", "NutritionistAlice", UserRoles.Nutritionist, new DateTime(1982, 9, 12), Gender.Female, null, null, null, "+1777888999"),
                ("Dr. David Kim", "nutritionist2@example.com", "NutritionistDavid", UserRoles.Nutritionist, new DateTime(1979, 6, 7), Gender.Male, null, null, null, "+1999888777"),
                
                // Admin
                ("Admin User", "admin@example.com", "AdminUser", UserRoles.Admin, new DateTime(1980, 1, 1), Gender.Male, null, null, null, "+1111222333")
            };

            foreach (var (fullName, email, userName, role, dateOfBirth, gender, subscriptionTier, subscriptionStatus, subscriptionExpiresAt, phoneNumber) in users)
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new ApplicationUser
                    {
                        FullName = fullName,
                        Email = email,
                        NormalizedEmail = email.ToUpper(),
                        UserName = userName,
                        NormalizedUserName = userName.ToUpper(),
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        PhoneNumber = phoneNumber,
                        DateOfBirth = dateOfBirth,
                        Gender = gender,
                        SubscriptionTier = subscriptionTier,
                        SubscriptionStatus = subscriptionStatus,
                        SubscriptionExpiresAt = subscriptionExpiresAt,
                        Status = UserStatus.Enabled,
                        CreatedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 30)),
                        UpdatedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 7)),
                        LastLoginAt = DateTime.Now.AddHours(-Random.Shared.Next(1, 72)),
                        
                        
                    };

                    var password = "123123";
                    var result = await userManager.CreateAsync(user, password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                    else
                    {
                        throw new Exception($"Failed to create user {userName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }
}