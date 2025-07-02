using DrHan.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Interfaces.Services
{
    public interface ISubscriptionService
    {
        Task<bool> HasActiveSubscription(int userId);
        Task<bool> CanUseFeature(int userId, string featureName, string limitType = "daily");
        Task<SubscriptionPlan> GetUserPlan(int userId);
        Task TrackUsage(int userId, string featureName, int count = 1);
        Task<int> GetUsageCount(int userId, string featureName, DateTime? fromDate = null);
        Task<Dictionary<string, object>> GetPlanLimits(SubscriptionPlan plan);
        Task RefreshPlanCache(); 
    }
}
