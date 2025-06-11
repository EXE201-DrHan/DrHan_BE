using DrHan.Application.Interfaces.Services.CacheService;
using DrHan.Infrastructure.ExternalServices.CacheService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            // Add Redis connection multiplexer
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var config = new ConfigurationOptions
                {
                    EndPoints = { redisOptions.ConnectionString },
                    AbortOnConnectFail = redisOptions.AbortOnConnectFail,
                    ConnectTimeout = redisOptions.ConnectTimeout,
                    SyncTimeout = redisOptions.SyncTimeout,
                    DefaultDatabase = redisOptions.Database
                };
                return ConnectionMultiplexer.Connect(config);
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
