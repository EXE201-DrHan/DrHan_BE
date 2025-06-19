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
            services.AddSingleton<ICacheKeyService, CacheKeyService>(provider => new CacheKeyService("drhan"));
            services.AddScoped<ICacheService, CacheService>();

            // Try to connect to Redis with proper error handling
            try
            {
                // Add Redis connection multiplexer with retry logic
                services.AddSingleton<IConnectionMultiplexer>(provider =>
                {
                    var logger = provider.GetService<ILogger<IConnectionMultiplexer>>();
                    try
                    {
                        var config = new ConfigurationOptions
                        {
                            EndPoints = { redisOptions.ConnectionString },
                            AbortOnConnectFail = false, // Don't fail if Redis is unavailable
                            ConnectTimeout = redisOptions.ConnectTimeout,
                            SyncTimeout = redisOptions.SyncTimeout,
                            DefaultDatabase = redisOptions.Database,
                            ConnectRetry = 3,
                            ReconnectRetryPolicy = new LinearRetry(5000)
                        };
                        
                        var multiplexer = ConnectionMultiplexer.Connect(config);
                        logger?.LogInformation("Successfully connected to Redis at {ConnectionString}", redisOptions.ConnectionString);
                        return multiplexer;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Failed to connect to Redis at {ConnectionString}. Falling back to memory cache only.", redisOptions.ConnectionString);
                        // Return a null multiplexer to indicate Redis is not available
                        return null;
                    }
                });

                // Add distributed cache with fallback
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisOptions.ConnectionString;
                    options.InstanceName = redisOptions.InstanceName;
                    options.ConfigurationOptions = new ConfigurationOptions
                    {
                        EndPoints = { redisOptions.ConnectionString },
                        AbortOnConnectFail = false, // Don't fail if Redis is unavailable
                        ConnectTimeout = redisOptions.ConnectTimeout,
                        SyncTimeout = redisOptions.SyncTimeout,
                        DefaultDatabase = redisOptions.Database,
                        ConnectRetry = 3,
                        ReconnectRetryPolicy = new LinearRetry(5000)
                    };
                });
            }
            catch (Exception ex)
            {
                // If Redis setup fails completely, log and continue with memory cache only
                var logger = services.BuildServiceProvider().GetService<ILogger<IConnectionMultiplexer>>();
                logger?.LogWarning(ex, "Redis setup failed completely. Application will use memory cache only.");
            }

            return services;
        }
    }
}