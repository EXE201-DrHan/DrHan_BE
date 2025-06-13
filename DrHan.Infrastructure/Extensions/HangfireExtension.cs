using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Hangfire.Redis.StackExchange;

namespace DrHan.Infrastructure.Extensions
{
    

    public static class HangfireConfiguration
    {
        public static IServiceCollection AddHangfireWithFallback(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger)
        {
            var redisConnection = configuration.GetConnectionString("Redis");
            var sqlConnection = configuration.GetConnectionString("DefaultConnection");

            try
            {
                // Try Redis first (preferred for performance)
                logger.LogInformation("Attempting to connect to Redis for Hangfire storage...");

                var redis = ConnectionMultiplexer.Connect(redisConnection);
                redis.GetDatabase(); // Test connection

                services.AddHangfire(config =>
                    config.UseRedisStorage(redisConnection));

                logger.LogInformation("Hangfire configured with Redis storage");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis connection failed, falling back to SQL Server storage");

                // Fallback to SQL Server
                services.AddHangfire(config =>
                    config.UseSqlServerStorage(sqlConnection, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    }));

                logger.LogInformation("Hangfire configured with SQL Server storage");
            }

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Environment.ProcessorCount * 2;
                options.Queues = new[] { "critical", "default", "background" };
            });
            
            return services;
        }
    }
}
