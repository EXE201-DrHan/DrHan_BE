using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services.AuthenticationServices;
using DrHan.Domain.Entities.Users;
using DrHan.Infrastructure.ExternalServices.AuthenticationService;
using DrHan.Infrastructure.Persistence;
using DrHan.Infrastructure.Repositories.HCP.Repository.GenericRepository;
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

        services.AddScoped<IUnitOfWork,UnitOfWork>();
        services.AddScoped(typeof(UserManager<>));
        services.AddScoped(typeof(IApplicationUserService<>), typeof(ApplicationUserService<>));
    }
    
}