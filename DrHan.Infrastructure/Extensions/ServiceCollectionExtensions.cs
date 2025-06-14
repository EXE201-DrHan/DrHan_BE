using DrHan.Application.Interfaces.Services;
using DrHan.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DrHan.Infrastructure.Services;
namespace DrHan.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // HTTP Clients
        services.AddHttpClient<IGeminiRecipeService, GeminiRecipeService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2); // Set timeout for Gemini API calls
            client.DefaultRequestHeaders.Add("User-Agent", "DrHan-Recipe-App/1.0");
        });
        
        // Recipe Cache Service (both as scoped service and hosted service)
        services.AddScoped<IRecipeCacheService, Infrastructure.Services.RecipeCacheService>();
        services.AddHostedService<Infrastructure.Services.RecipeCacheService>();
        
        // OTP and Push Notification Services
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddHostedService<OtpCleanupService>();
        
        // Ingredient Service
        services.AddScoped<IIngredientService, IngredientService>();
    }
} 