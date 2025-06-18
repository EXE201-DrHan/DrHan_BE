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
            IConfiguration configuration)
        {
            // Get Redis connection from the correct configuration path
            var redisConnection = configuration.GetSection("Redis:ConnectionString").Value;
            var sqlConnection = configuration.GetConnectionString("DefaultConnection");

            // Always try SQL Server first for simplicity since Redis Cloud has SSL issues with Hangfire
            // For production, SQL Server is more reliable for Hangfire anyway
            services.AddHangfire(config =>
            {
                if (!string.IsNullOrEmpty(redisConnection) && redisConnection.Contains("localhost"))
                {
                    // Use Redis only for local development
                    try
                    {
                        var redis = ConnectionMultiplexer.Connect(redisConnection);
                        redis.GetDatabase(); // Test connection
                        config.UseRedisStorage(redisConnection);
                    }
                    catch (Exception)
                    {
                        // Fallback to SQL Server if local Redis fails
                        config.UseSqlServerStorage(sqlConnection, new SqlServerStorageOptions
                        {
                            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                            QueuePollInterval = TimeSpan.Zero,
                            UseRecommendedIsolationLevel = true,
                            DisableGlobalLocks = true
                        });
                    }
                }
                else
                {
                    // Use SQL Server for production (more reliable)
                    config.UseSqlServerStorage(sqlConnection, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    });
                }
            });

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Environment.ProcessorCount * 2;
                options.Queues = new[] { "critical", "default", "background" };
            });
            
            return services;
        }
    }
}
