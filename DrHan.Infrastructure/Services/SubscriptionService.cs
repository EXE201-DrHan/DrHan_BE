using DrHan.Application.Interfaces.Services;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;
using DrHan.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SubscriptionService> _logger;
        private const string PLAN_CACHE_KEY = "subscription_plan_features";
        private const int CACHE_DURATION_MINUTES = 30;

        public SubscriptionService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<SubscriptionService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> HasActiveSubscription(int userId)
        {
            var subscription = await _context.UserSubscriptions
                .Where(s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active)
                .FirstOrDefaultAsync();

            return subscription != null &&
                   (subscription.EndDate == null || subscription.EndDate > DateTime.UtcNow);
        }

        public async Task<SubscriptionPlan> GetUserPlan(int userId)
        {
            var subscription = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active)
                .FirstOrDefaultAsync();

            if (subscription?.Plan != null)
                return subscription.Plan;

            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Name.ToLower() == "free") 
                ?? throw new InvalidOperationException("No free plan found in the system");
        }

        public async Task<bool> CanUseFeature(int userId, string featureName, string limitType = "daily")
        {
            try
            {
                var plan = await GetUserPlan(userId);
                var planFeatures = await GetPlanFeaturesFromCache(plan);

                if (!planFeatures.ContainsKey(featureName))
                {
                    _logger.LogWarning("Feature {FeatureName} not configured for plan {PlanName}", featureName, plan.Name);
                    return false;
                }

                var feature = planFeatures[featureName];

                if (!feature.IsEnabled)
                    return false;

                if (plan.UsageQuota == null)
                    return true;

                var fromDate = limitType.ToLower() == "monthly"
                    ? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
                    : DateTime.UtcNow.Date;

                var currentUsage = await GetUsageCount(userId, featureName, fromDate);

                return currentUsage < plan.UsageQuota.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feature access for user {UserId}, feature {FeatureName}", userId, featureName);
                return false; 
            }
        }

        public async Task TrackUsage(int userId, string featureName, int count = 1)
        {
            try
            {
                var userSubscription = await _context.UserSubscriptions
                    .Where(s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active)
                    .FirstOrDefaultAsync();

                if (userSubscription == null)
                {
                    _logger.LogWarning("No active subscription found for user {UserId}", userId);
                    return;
                }

                var today = DateTime.UtcNow.Date;
                var existingRecord = await _context.SubscriptionUsages
                    .FirstOrDefaultAsync(u => u.UserSubscriptionId == userSubscription.Id &&
                                             u.FeatureType == featureName &&
                                             u.UsageDate.Date == today);

                if (existingRecord != null)
                {
                    existingRecord.UsageCount += count;
                }
                else
                {
                    _context.SubscriptionUsages.Add(new SubscriptionUsage
                    {
                        UserSubscriptionId = userSubscription.Id,
                        FeatureType = featureName,
                        UsageCount = count,
                        UsageDate = DateTime.UtcNow,
                        ResourceUsed = $"Feature usage tracked at {DateTime.UtcNow}"
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking usage for user {UserId}, feature {FeatureName}", userId, featureName);
            }
        }

        public async Task<int> GetUsageCount(int userId, string featureName, DateTime? fromDate = null)
        {
            try
            {
                var userSubscription = await _context.UserSubscriptions
                    .Where(s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active)
                    .FirstOrDefaultAsync();

                if (userSubscription == null)
                    return 0;

                var query = _context.SubscriptionUsages
                    .Where(u => u.UserSubscriptionId == userSubscription.Id && u.FeatureType == featureName);

                if (fromDate.HasValue)
                    query = query.Where(u => u.UsageDate >= fromDate.Value);

                return await query.SumAsync(u => u.UsageCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage count for user {UserId}, feature {FeatureName}", userId, featureName);
                return 0;
            }
        }

        public async Task<Dictionary<string, object>> GetPlanLimits(SubscriptionPlan plan)
        {
            var planFeatures = await GetPlanFeaturesFromCache(plan);

            return planFeatures.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)new
                {
                    isEnabled = kvp.Value.IsEnabled,
                    usageQuota = plan.UsageQuota,
                    description = kvp.Value.Description
                }
            );
        }

        private async Task<Dictionary<string, PlanFeature>> GetPlanFeaturesFromCache(SubscriptionPlan plan)
        {
            var cacheKey = $"{PLAN_CACHE_KEY}_{plan.Id}";

            if (_cache.TryGetValue(cacheKey, out Dictionary<string, PlanFeature> cachedFeatures))
            {
                return cachedFeatures;
            }

            var features = await _context.SubscriptionPlans
                .Where(sp => sp.Id == plan.Id)
                .SelectMany(sp => sp.PlanFeatures)
                .ToDictionaryAsync(pf => pf.FeatureName, pf => pf);

            _cache.Set(cacheKey, features, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return features;
        }

        public async Task RefreshPlanCache()
        {
            try
            {
                var allPlans = await _context.SubscriptionPlans.ToListAsync();
                
                foreach (var plan in allPlans)
                {
                    var cacheKey = $"{PLAN_CACHE_KEY}_{plan.Id}";
                    _cache.Remove(cacheKey);
                    
                    await GetPlanFeaturesFromCache(plan);
                }
                
                _logger.LogInformation("Subscription plan cache refreshed for {PlanCount} plans", allPlans.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing plan cache");
            }
        }
    }
}
