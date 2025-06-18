using DrHan.Application.Interfaces.Services.CacheService;
using DrHan.Infrastructure.ExternalServices.CacheService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.Extensions
{
    public static class RedisExtension
    {
        public static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration)
        {
            var redisOptions = configuration.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();
            var cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();

            services.Configure<RedisOptions>(configuration.GetSection("Redis"));
            services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));
            services.AddMemoryCache();
            services.AddSingleton<ICacheKeyService,CacheKeyService>(provider => new CacheKeyService("drhan"));
            services.AddScoped<ICacheService, CacheService>();
            
            // Add Redis connection multiplexer with better error handling
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();
                
                try
                {
                    logger.LogInformation("Attempting to connect to Redis: {ConnectionString}", 
                        redisOptions.ConnectionString?.Split(',')[0]); // Log only the endpoint, not the password
                    
                    var config = ConfigurationOptions.Parse(redisOptions.ConnectionString);
                    config.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
                    config.ConnectTimeout = redisOptions.ConnectTimeout;
                    config.SyncTimeout = redisOptions.SyncTimeout;
                    config.DefaultDatabase = redisOptions.Database;
                    config.ConnectRetry = 3;
                    config.ReconnectRetryPolicy = new ExponentialRetry(100);
                    
                    var multiplexer = ConnectionMultiplexer.Connect(config);
                    
                    // Test the connection
                    var database = multiplexer.GetDatabase();
                    database.Ping();
                    
                    logger.LogInformation("Successfully connected to Redis");
                    return multiplexer;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to connect to Redis. Using in-memory fallback.");
                    // Return a null multiplexer - handle this in CacheService
                    throw;
                }
            });

            // Add distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.ConnectionString;
                options.InstanceName = redisOptions.InstanceName;
                options.ConfigurationOptions = new ConfigurationOptions
                {
                    EndPoints = {redisOptions.ConnectionString},
                    AbortOnConnectFail = redisOptions.AbortOnConnectFail,
                    ConnectTimeout = redisOptions.ConnectTimeout,
                    SyncTimeout = redisOptions.SyncTimeout,
                    DefaultDatabase = redisOptions.Database
                };
            });

            return services;
        }
    }
}
