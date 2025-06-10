using DrHan.Application.Interfaces;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Application.Interfaces.Services.CacheService;
using DrHan.Domain.Entities.Users;
using DrHan.Infrastructure.ExternalServices;
using DrHan.Infrastructure.ExternalServices.AuthenticationService;
using DrHan.Infrastructure.ExternalServices.CacheService;
using DrHan.Infrastructure.Persistence;
using DrHan.Infrastructure.Repositories.HCP.Repository.GenericRepository;
using DrHan.Infrastructure.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DrHan.Infrastructure.Extensions;

public static class DatabaseExtension
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .EnableSensitiveDataLogging());

        services
            .AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 3; 
            options.Password.RequiredUniqueChars = 0;

            options.User.RequireUniqueEmail = false;
        });
        services.AddScoped<IUnitOfWork,UnitOfWork>();
        services.AddScoped(typeof(UserManager<>));
        services.AddScoped(typeof(RoleManager<>));

        services.AddScoped(typeof(IApplicationUserService<>), typeof(ApplicationUserService<>));
        
        // Register authentication services
        services.AddScoped<IUserTokenService, UserTokenService>();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IEmailService, EmailService>();
        
        // Register data management service
        services.AddScoped<DrHan.Infrastructure.Seeders.DataManagementService>();
    }
    
}