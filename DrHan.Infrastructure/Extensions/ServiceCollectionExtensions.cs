using DrHan.Application.Interfaces.Services;
using DrHan.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DrHan.Infrastructure.Services;
using DrHan.Application.DTOs.Payment;
using Net.payOS;
using DrHan.Application.Services.ValidationServices;
using DrHan.Application.Services.SmartScoringService;
namespace DrHan.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // HTTP Clients
        services.AddHttpClient<IGeminiRecipeService, GeminiRecipeService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2); 
            client.DefaultRequestHeaders.Add("User-Agent", "DrHan-Recipe-App/1.0");
        });
        
        // Recipe Cache Service (both as scoped service and hosted service)
        services.AddScoped<IRecipeCacheService, Infrastructure.Services.RecipeCacheService>();
        services.AddHostedService<Infrastructure.Services.RecipeCacheService>();
        services.AddScoped<ISmartMealPlanService, SmartMealPlanService>(); 
        
        // OTP and Push Notification Services
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddHostedService<OtpCleanupService>();
        services.AddScoped<IMealTypeValidationService, MealTypeValidationService>();
        services.AddScoped<IMealNotificationService, MealNotificationService>();
        services.AddHostedService<Infrastructure.BackgroundServices.MealNotificationBackgroundService>();
        services.AddScoped<ISmartScoringService, SmartScoringService>();
        services.AddScoped<IRecommendNewService, RecommendNewService>();
        services.AddScoped<IVisionService, VisionService>();
        services.AddScoped<IFoodRecognitionService, FoodRecognitionService>();
        services.AddScoped<IMappingService, MappingService>(); 

        // Payment Services
        //services.AddScoped<IPayOSService, PayOSService>();

        // Subscription Service
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        //services.AddHostedService<SubscriptionExpiryService>();
        
        // Ingredient Service
    }
    public static IServiceCollection AddPayOSService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure PayOS settings
        services.Configure<PayOSConfiguration>(configuration.GetSection("PayOS"));

        // Register HttpClient for PayOS with proper configuration
        services.AddHttpClient("PayOS", client =>
        {
            var payOSConfig = configuration.GetSection("PayOS").Get<PayOSConfiguration>();
            if (payOSConfig != null)
            {
                client.BaseAddress = new Uri(payOSConfig.BaseUrl);
                client.DefaultRequestHeaders.Add("x-client-id", payOSConfig.ClientId);
                client.DefaultRequestHeaders.Add("x-api-key", payOSConfig.ApiKey);
            }
        });
        var payOSConfig = configuration.GetSection("PayOS").Get<PayOSConfiguration>();

        PayOS payOS = new PayOS(payOSConfig.ClientId ?? throw new Exception("Cannot find environment"),
                    payOSConfig.ApiKey ?? throw new Exception("Cannot find environment"),
                    payOSConfig.ChecksumKey ?? throw new Exception("Cannot find environment"));

        // Register the service
        services.AddScoped<IPayOSService, PayOSService>();
        services.AddSingleton(payOS);

        return services;
    }
} 